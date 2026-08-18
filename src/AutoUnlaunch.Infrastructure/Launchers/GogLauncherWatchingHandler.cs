using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using System.Diagnostics;
using System.Management;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal partial class GogLauncherWatchingHandler : LauncherWatchingHandler
{
    private const string LauncherProcessName = "GalaxyClient";
    private const string RegistryRootPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient";

    private readonly GogSettingsService _gogSettingsService;
    private readonly ProcessWindowService _processWindowService;
    private readonly ILogger<GogLauncherWatchingHandler> _logger;
    private readonly SemaphoreSlim _lock = new(0, 1);
    private readonly Dictionary<long, string> _installPaths = [];

    public GogLauncherWatchingHandler(IProcessWatcher processWatcher,
        GogSettingsService gogSettingsService,
        ProcessWindowService processWindowService,
        ILogger<GogLauncherWatchingHandler> logger) : base(processWatcher, gogSettingsService, logger)
    {
        _gogSettingsService = gogSettingsService;
        _processWindowService = processWindowService;
        _logger = logger;

        var query = """
            SELECT *
            FROM RegistryTreeChangeEvent
            WHERE Hive = 'HKEY_LOCAL_MACHINE'
            AND RootPath = 'SOFTWARE\\WOW6432Node\\GOG.com'
            """;
        var registryTreeWatcher = new ManagementEventWatcher(@"\\.\root\default", query);
        registryTreeWatcher.EventArrived += async (sender, e) =>
        {
            await _lock.WaitAsync();

            try
            {
                UpdateInstallPaths();
            }
            finally
            {
                _lock.Release();
            }
        };
        registryTreeWatcher.Start();

        UpdateInstallPaths();

        _lock.Release();
    }

    public override string LauncherName => "GOG Galaxy";

    protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
    {
        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        return Task.FromResult(launcherProcessesResult.Items.Any());
    }

    protected override async Task<bool> IsLauncherActivityAsync(ProcessInfo processInfo)
    {
        var processPath = !string.IsNullOrWhiteSpace(processInfo.ProcessPath)
            ? Path.GetFullPath(processInfo.ProcessPath)
            : null;

        if (string.IsNullOrEmpty(processPath))
            return false;

        await _lock.WaitAsync();

        try
        {
            return _installPaths.Values.Any(x => processPath.StartsWith(Path.GetFullPath(x), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _lock.Release();
        }
    }

    protected override async Task StopLauncherAsync(CancellationToken cancellationToken)
    {
        var stopMethod = _gogSettingsService.GetLauncherStopMethod();
        switch (stopMethod)
        {
            case LauncherStopMethod.KillProcess:
                using (var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName))
                {
                    foreach (var process in launcherProcessesResult.Items)
                    {
                        _logger.LogKillingProcess(process.ProcessName, process.Id);
                        process.Kill();
                    }
                }
                break;
            case LauncherStopMethod.RequestShutdown:
                await RequestLauncherShutdown(cancellationToken);
                break;
            default:
                _logger.LogError("Stop method {StopMethod} is not supported for {LauncherName}.",
                    stopMethod,
                    LauncherName);
                break;
        }
    }

    protected override async Task OnLauncherActivityStarted(CancellationToken cancellationToken)
    {
        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        foreach (var process in launcherProcessesResult.Items)
        {
            await _processWindowService.EnsureWindowsClosedAsync(process.Id);
        }
    }

    private void UpdateInstallPaths()
    {
        _logger.LogInformation("Updating GOG game install locations.");

        _installPaths.Clear();

        using var gamesSubKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\WOW6432Node\GOG.com\Games");
        if (gamesSubKey is null)
        {
            _logger.LogWarning("Could not open GOG games registry sub key.");
            return;
        }

        foreach (var gameSubKeyName in gamesSubKey.GetSubKeyNames())
        {
            if (!long.TryParse(gameSubKeyName, out var gogGameId))
            {
                _logger.LogWarning("Skipping registry sub key {GogGameSubKeyName} becuase it is not a valid GOG game ID.", gameSubKeyName);
                continue;
            }

            using var gameSubKey = gamesSubKey.OpenSubKey(gameSubKeyName);
            if (gameSubKey is null)
            {
                _logger.LogWarning("Could not open GOG game registry sub key {GogGameSubKeyName}.", gameSubKeyName);
                continue;
            }

            var path = gameSubKey.GetValue("path")?.ToString();
            if (string.IsNullOrEmpty(path))
            {
                _logger.LogWarning("GOG game registry sub key {GogGameSubKeyName} does not have an entry for 'path'.",
                    gameSubKeyName);
                continue;
            }

            LogFoundGame(gogGameId, path);

            _installPaths[gogGameId] = path;
        }
    }

    private async Task RequestLauncherShutdown(CancellationToken cancellationToken)
    {
        var launcherPath = Registry.GetValue($@"{RegistryRootPath}\paths", "client", null)?.ToString();
        var launcherExecutable = Registry.GetValue(RegistryRootPath, "clientExecutable", null)?.ToString();
        if (launcherPath is null || launcherExecutable == null)
        {
            _logger.LogError("Could not determine {LauncherName} executable path.", LauncherName);
            return;
        }

        try
        {
            var shutdownCommand = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = Path.Combine(launcherPath, launcherExecutable),
                    Arguments = "/command=shutdown"
                }
            };
            shutdownCommand.Start();
            await shutdownCommand.WaitForExitAsync(cancellationToken);
            LogGracefulShutdownSucceeded(LauncherName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request to gracefully shutdown {LauncherName} failed.", LauncherName);
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found GOG game {GogGameId} installed at {GogGameInstallPath}.")]
    private partial void LogFoundGame(long gogGameId, string gogGameInstallPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request to gracefully shutdown {LauncherName} succeeded.")]
    private partial void LogGracefulShutdownSucceeded(string launcherName);
}
