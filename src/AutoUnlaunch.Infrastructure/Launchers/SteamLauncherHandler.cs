using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal class SteamLauncherHandler(TimeProvider timeProvider,
    SteamSettingsService steamSettingsService,
    IProtocolLauncher protocolLauncher,
    ProcessWindowService processWindowService,
    ILogger<SteamLauncherHandler> logger)
    : LauncherHandler(steamSettingsService, timeProvider, logger)
{
    private const string LauncherProcessName = "steam";
    private const string WebHelperProcessName = "steamwebhelper";
    private static readonly Uri s_exitUri = new($"{LauncherUriProtocols.Steam}exit");

    private readonly SteamSettingsService _steamSettingsService = steamSettingsService;
    private readonly IProtocolLauncher _protocolLauncher = protocolLauncher;
    private readonly ProcessWindowService _processWindowService = processWindowService;

    protected override string LauncherName => "Steam";

    protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
    {
        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        return Task.FromResult(launcherProcessesResult.Items.Any());
    }

    protected override Task<bool> IsLauncherActivityRunningAsync(CancellationToken cancellationToken)
    {
        var activeSteamAppId = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "RunningAppID", 0) as int? ?? 0;
        return Task.FromResult(activeSteamAppId != 0);
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
                        _logger.LogInformation("Killing process {ProcessName} ({ProcessId}).",
                            process.ProcessName,
                            process.Id);
                        process.Kill();
                    }
                }
                break;
            case LauncherStopMethod.RequestShutdown:
                if (await _protocolLauncher.LaunchUriAsync(s_exitUri))
                {
                    _logger.LogInformation("Request to gracefully shutdown {LauncherName} succeeded.", LauncherName);

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
            .Where(x => x.GetParentProcessId() == launcherProcess.Id)
            .FirstOrDefault()
            ?.Id;
    }
}
