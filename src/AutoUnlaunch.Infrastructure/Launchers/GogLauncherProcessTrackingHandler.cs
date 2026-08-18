using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using System.Diagnostics;
using System.Management;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal partial class GogLauncherProcessTrackingHandler(IProcessWatcher processWatcher,
    GogSettingsService gogSettingsService,
    ProcessWindowService processWindowService,
    TimeProvider timeProvider,
    ILogger<GogLauncherProcessTrackingHandler> logger)
    : LauncherProcessTrackingHandler(processWatcher, gogSettingsService, timeProvider, logger)
{
    private const string LauncherProcessName = "GalaxyClient";
    private const string RegistryRootPath = @"SOFTWARE\GOG.com";
    private const string RegistryWatcherQuery = """
        SELECT *
        FROM RegistryTreeChangeEvent
        WHERE Hive = 'HKEY_LOCAL_MACHINE'
        AND RootPath = 'SOFTWARE\\WOW6432Node\\GOG.com'
        """;

    private readonly GogSettingsService _gogSettingsService = gogSettingsService;
    private readonly ProcessWindowService _processWindowService = processWindowService;
    private readonly ILogger<GogLauncherProcessTrackingHandler> _logger = logger;
    // TODO: Move registry watcher logic to shareable service. Also, maybe use win32 instead of management?
    private readonly ManagementEventWatcher _registryTreeWatcher = new(new ManagementScope(@"root\default"), new EventQuery(RegistryWatcherQuery));
    private readonly SemaphoreSlim _lock = new(1);
    private readonly Dictionary<long, string> _installPaths = [];

    public override string LauncherName => "GOG Galaxy";

    protected override async Task StartCoreAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        try
        {
            _registryTreeWatcher.EventArrived -= RegistryTreeWatcher_EventArrived;
            _registryTreeWatcher.EventArrived += RegistryTreeWatcher_EventArrived;
            _registryTreeWatcher.Start();

            UpdateInstallPaths();
        }
        finally
        {
            _lock.Release();
        }
    }

    protected override Task StopCoreAsync(CancellationToken cancellationToken = default)
    {
        _registryTreeWatcher.EventArrived -= RegistryTreeWatcher_EventArrived;
        _registryTreeWatcher.Stop();

        return Task.CompletedTask;
    }

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

        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
        using var gamesSubKey = baseKey.OpenSubKey($@"{RegistryRootPath}\Games");
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
        using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);

        var launcherPath = baseKey.GetValue($@"{RegistryRootPath}\GalaxyClient\paths", "client")?.ToString();
        var launcherExecutable = baseKey.GetValue($@"{RegistryRootPath}\GalaxyClient", "clientExecutable")?.ToString();
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

    private async void RegistryTreeWatcher_EventArrived(object sender, EventArrivedEventArgs e)
    {
        await _lock.WaitAsync();

        try
        {
            UpdateInstallPaths();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while updating GOG game install paths.");
        }
        finally
        {
            _lock.Release();
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found GOG game {GogGameId} installed at {GogGameInstallPath}.")]
    private partial void LogFoundGame(long gogGameId, string gogGameInstallPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Request to gracefully shutdown {LauncherName} succeeded.")]
    private partial void LogGracefulShutdownSucceeded(string launcherName);
}
