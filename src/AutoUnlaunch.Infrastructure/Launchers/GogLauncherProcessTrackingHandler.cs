using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal partial class GogLauncherProcessTrackingHandler(IProcessWatcher processWatcher,
    GogSettingsService gogSettingsService,
    TimeProvider timeProvider,
    RegistryWatcherFactory registryWatcherFactory,
    ProcessWindowService processWindowService,
    ILogger<GogLauncherProcessTrackingHandler> logger)
    : LauncherProcessTrackingHandler(processWatcher, gogSettingsService, timeProvider, logger), IAsyncDisposable
{
    private const string LauncherProcessName = "GalaxyClient";
    private const string RegistryRootPath = @"SOFTWARE\GOG.com";

    private static readonly RegistryHive s_registryHive = RegistryHive.LocalMachine;
    private static readonly RegistryView s_registryView = RegistryView.Registry32;

    private readonly GogSettingsService _gogSettingsService = gogSettingsService;
    private readonly RegistryWatcher _registryWatcher = registryWatcherFactory.Create(s_registryHive,
        @$"{RegistryRootPath}\Games",
        s_registryView);
    private readonly ProcessWindowService _processWindowService = processWindowService;
    private readonly ILogger<GogLauncherProcessTrackingHandler> _logger = logger;
    private readonly SemaphoreSlim _lock = new(1);
    private readonly Dictionary<long, string> _installPaths = [];

    public override string LauncherName => "GOG Galaxy";

    protected override async Task StartCoreAsync(CancellationToken cancellationToken = default)
    {
        _registryWatcher.Changed += RegistryWatcher_Changed;
        _registryWatcher.Errored += RegistryWatcher_Errored;

        try
        {
            _registryWatcher.Start();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start registry watcher. GOG games list will not be refreshed until handler is restarted.");
        }

        await UpdateInstallPathsAsync(cancellationToken);
    }

    protected override Task StopCoreAsync(CancellationToken cancellationToken = default)
    {
        _registryWatcher.Changed -= RegistryWatcher_Changed;
        _registryWatcher.Errored -= RegistryWatcher_Errored;
        _registryWatcher.Stop();

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

    private async Task UpdateInstallPathsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        _logger.LogInformation("Updating GOG game install locations.");

        _installPaths.Clear();

        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(s_registryHive, s_registryView);
            using var gamesSubKey = baseKey.OpenSubKey($@"{RegistryRootPath}\Games");
            if (gamesSubKey is null)
            {
                _logger.LogWarning("Could not open GOG games registry sub key.");
                return;
            }

            foreach (var gameSubKeyName in gamesSubKey.GetSubKeyNames())
            {
                try
                {
                    if (!long.TryParse(gameSubKeyName, out var gogGameId))
                    {
                        _logger.LogWarning("Skipping registry sub key '{RegistrySubKeyName}' because it is not a valid GOG game ID.", gameSubKeyName);
                        continue;
                    }

                    using var gameSubKey = gamesSubKey.OpenSubKey(gameSubKeyName);
                    if (gameSubKey is null)
                    {
                        _logger.LogWarning("Could not open GOG game registry sub key '{RegistrySubKeyName}'.", gameSubKeyName);
                        continue;
                    }

                    var path = gameSubKey.GetValue("path")?.ToString();
                    if (string.IsNullOrEmpty(path))
                    {
                        _logger.LogWarning("GOG game registry sub key '{RegistrySubKeyName}' does not have an entry for 'path'.",
                            gameSubKeyName);
                        continue;
                    }

                    var gameName = gameSubKey.GetValue("gameName")?.ToString() ?? "Unknown Game";

                    try
                    {
                        var normalizedInstallPath = Path.GetFullPath(path);
                        _installPaths[gogGameId] = normalizedInstallPath;
                        LogFoundGame(gameName, gogGameId, normalizedInstallPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Found GOG game '{GogGameName}' ({GogGameId}) but something went wrong while trying to track its install location of {GogGameInstallPath}.",
                            gameName,
                            gogGameId,
                            path);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Found GOG game registry sub key '{RegistrySubKeyName}' but something went wrong while trying to read it.",
                        gameSubKeyName);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while updating Epic game install paths. Game detection may not work properly.");
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task RequestLauncherShutdown(CancellationToken cancellationToken)
    {
        using var baseKey = RegistryKey.OpenBaseKey(s_registryHive, s_registryView);

        using var pathsKey = baseKey.OpenSubKey($@"{RegistryRootPath}\GalaxyClient\paths");
        var launcherPath = pathsKey?.GetValue("client")?.ToString();

        using var clientKey = baseKey.OpenSubKey($@"{RegistryRootPath}\GalaxyClient");
        var launcherExecutable = clientKey?.GetValue("clientExecutable")?.ToString();

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
            _logger.LogGracefulShutdownSucceeded(LauncherName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request to gracefully shutdown {LauncherName} failed.", LauncherName);
        }
    }

    private async void RegistryWatcher_Changed(object? sender, EventArgs e)
    {
        await UpdateInstallPathsAsync();
    }

    private void RegistryWatcher_Errored(object? sender, EventArgs e)
    {
        _logger.LogWarning("Registry watcher encountered an unexpected error and has stopped. GOG games list will not be refreshed until handler is restarted.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _registryWatcher.Dispose();
        _lock.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Found GOG game '{GogGameName}' ({GogGameId}) installed at {GogGameInstallPath}.")]
    private partial void LogFoundGame(string gogGameName, long gogGameId, string gogGameInstallPath);
}
