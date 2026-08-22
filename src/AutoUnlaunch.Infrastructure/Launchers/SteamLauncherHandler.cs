using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal partial class SteamLauncherHandler(SteamSettingsService steamSettingsService,
    TimeProvider timeProvider,
    RegistryWatcherFactory registryWatcherFactory,
    IProtocolLauncher protocolLauncher,
    ProcessWindowService processWindowService,
    ILogger<SteamLauncherHandler> logger)
    : LauncherBaseHandler(steamSettingsService, timeProvider, logger), IAsyncDisposable
{
    private const string LauncherProcessName = "steam";
    private const string WebHelperProcessName = "steamwebhelper";
    private const RegistryHive RegistryHive = RegistryHive.CurrentUser;
    private const string RegistryRootPath = @"Software\Valve\Steam";
    private const RegistryView RegistryView = RegistryView.Registry64;

    private static readonly Uri s_exitUri = new($"{LauncherUriProtocols.Steam}exit");

    private readonly SteamSettingsService _steamSettingsService = steamSettingsService;
    private readonly RegistryWatcher _registryWatcher = registryWatcherFactory.Create(RegistryHive,
        RegistryRootPath,
        RegistryView);
    private readonly IProtocolLauncher _protocolLauncher = protocolLauncher;
    private readonly ProcessWindowService _processWindowService = processWindowService;
    private readonly ILogger<SteamLauncherHandler> _logger = logger;

    private int _activeSteamAppId = 0;

    public override string LauncherName => "Steam";

    protected override async Task OnStartingAsync(CancellationToken cancellationToken = default)
    {
        _registryWatcher.Changed += RegistryWatcher_Changed;
        _registryWatcher.Errored += RegistryWatcher_Errored;

        try
        {
            _registryWatcher.Start();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start registry watcher. Steam activity detection will not work until handler is restarted.");
        }

        await CheckCurrentRunningSteamAppAsync(cancellationToken);
        await base.OnStartingAsync(cancellationToken);
    }

    protected override Task OnStoppingAsync(CancellationToken cancellationToken = default)
    {
        _registryWatcher.Changed -= RegistryWatcher_Changed;
        _registryWatcher.Errored -= RegistryWatcher_Errored;
        _registryWatcher.Stop();

        return base.OnStoppingAsync(cancellationToken);
    }

    protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
    {
        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        return Task.FromResult(launcherProcessesResult.Items.Any());
    }

    protected override async Task StopLauncherAsync(CancellationToken cancellationToken)
    {
        var stopMethod = _steamSettingsService.GetLauncherStopMethod();
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
                if (await _protocolLauncher.LaunchUriAsync(s_exitUri))
                {
                    _logger.LogGracefulShutdownSucceeded(LauncherName);

                    if (_steamSettingsService.GetHidesShutdownScreen() != true)
                        return;

                    // After requesting an exit, the Steam UI will show the main window and a shutting down splash
                    // screen. Close all main windows for this process until the process is no longer running or
                    // until timing out after 1 second.
                    var mainUIProcessId = GetMainUIProcessId();
                    if (mainUIProcessId is null)
                    {
                        _logger.LogError("Could not find main UI process for {LauncherName}.", LauncherName);
                        return;
                    }

                    await _processWindowService.EnsureWindowsClosedAsync(mainUIProcessId.Value, continueUntilTimeout: true);
                }
                else
                    _logger.LogError("Request to gracefully shutdown {LauncherName} failed.", LauncherName);
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
        if (_steamSettingsService.GetHidesOnActivityStart() != true)
            return;

        await CloseMainWindowsAsync();
    }

    protected override async Task OnLauncherActivityEnded(CancellationToken cancellationToken)
    {
        if (_steamSettingsService.GetHidesOnActivityEnd() != true)
            return;

        await CloseMainWindowsAsync();
    }

    private async Task CheckCurrentRunningSteamAppAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var oldActiveSteamAppId = _activeSteamAppId;

            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive, RegistryView);
            using var steamKey = baseKey.OpenSubKey(RegistryRootPath);
            _activeSteamAppId = steamKey?.GetValue("RunningAppID", 0) as int? ?? 0;

            if (oldActiveSteamAppId != _activeSteamAppId)
                LogRunningSteamAppChanged(_activeSteamAppId);

            if (oldActiveSteamAppId == 0 && _activeSteamAppId != 0)
            {
                LogActivityStarted(LauncherName, _activeSteamAppId);
                await SetLauncherActivityStartedAsync(cancellationToken);
            }
            else if (oldActiveSteamAppId != 0 && _activeSteamAppId == 0)
                await SetLauncherActivityEndedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while checking for the currently running Steam app.");
        }
    }

    private async Task CloseMainWindowsAsync()
    {
        // The Steam UI may show different windows (commonly splash screens) as its main window so close the
        // current one and repeat until there are no more main windows or until timing out after 1 second.
        var mainUIProcessId = GetMainUIProcessId();
        if (mainUIProcessId is null)
        {
            _logger.LogError("Could not find main UI process for {LauncherName}.", LauncherName);
            return;
        }

        await _processWindowService.EnsureWindowsClosedAsync(mainUIProcessId.Value);
    }

    private static int? GetMainUIProcessId()
    {
        // Look for the main Steam UI process by searching for the sole steamwebhelper process where its parent process
        // is the steam process.
        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        var launcherProcess = launcherProcessesResult.Items.FirstOrDefault();
        if (launcherProcess is null)
            return null;

        using var webHelperProcessesResult = ProcessHelper.GetSessionProcessesByName(WebHelperProcessName);
        return webHelperProcessesResult.Items
            .FirstOrDefault(x => x.GetParentProcessId() == launcherProcess.Id)
            ?.Id;
    }

    private async void RegistryWatcher_Changed(object? sender, EventArgs e) => await CheckCurrentRunningSteamAppAsync();

    private void RegistryWatcher_Errored(object? sender, EventArgs e)
    {
        _logger.LogWarning("Registry watcher encountered an unexpected error and has stopped. Steam activity detection will not work until handler is restarted.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _registryWatcher.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Running Steam app ID changed to {SteamAppId}.")]
    private partial void LogRunningSteamAppChanged(long steamAppId);

    [LoggerMessage(Level = LogLevel.Information, Message = "An activity for launcher {LauncherName} started with Steam app ID {SteamAppId}.")]
    private partial void LogActivityStarted(string launcherName, long steamAppId);
}
