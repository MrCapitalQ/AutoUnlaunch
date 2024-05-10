using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal class SteamLauncherHandler : LauncherHandler
{
    private const string LauncherProcessName = "steam";
    private const string WebHelperProcessName = "steamwebhelper";
    private static readonly Uri s_exitUri = new($"{LauncherUriProtocols.Steam}exit");

    private readonly SteamSettingsService _steamSettingsService;
    private readonly IProtocolLauncher _protocolLauncher;

    public SteamLauncherHandler(TimeProvider timeProvider,
        SteamSettingsService steamSettingsService,
        IProtocolLauncher protocolLauncher,
        ILogger<SteamLauncherHandler> logger)
        : base(steamSettingsService, timeProvider, logger)
    {
        _steamSettingsService = steamSettingsService;
        _protocolLauncher = protocolLauncher;
    }

    protected override string LauncherName => "Steam";

    protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
        => Task.FromResult(ProcessHelper.GetSessionProcessesByName(LauncherProcessName).Any());

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
                foreach (var process in ProcessHelper.GetSessionProcessesByName(LauncherProcessName))
                {
                    _logger.LogInformation("Killing process {ProcessName} ({ProcessId}).",
                        process.ProcessName,
                        process.Id);
                    process.Kill();
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

                    var timeout = DateTimeOffset.UtcNow.AddSeconds(1);
                    while (DateTimeOffset.UtcNow < timeout)
                    {
                        try
                        {
                            var process = Process.GetProcessById(mainUIProcessId.Value);

                            if (process.MainWindowHandle == 0)
                            {
                                await Task.Delay(50, cancellationToken);
                                continue;
                            }

                            _logger.LogInformation("Closing current main window with title '{WindowTitle}' for process {ProcessName} ({ProcessId}).",
                                process.MainWindowTitle,
                                process.ProcessName,
                                process.Id);

                            process.CloseMainWindow();
                        }
                        catch
                        {
                            break;
                        }
                    }
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

    protected override Task OnLauncherActivityStarted(CancellationToken cancellationToken)
    {
        if (_steamSettingsService.GetHidesOnActivityStart() != true)
            return Task.CompletedTask;

        CloseMainWindows();
        return Task.CompletedTask;
    }

    protected override Task OnLauncherActivityEnded(CancellationToken cancellationToken)
    {
        if (_steamSettingsService.GetHidesOnActivityEnd() != true)
            return Task.CompletedTask;

        CloseMainWindows();
        return Task.CompletedTask;
    }

    private void CloseMainWindows()
    {
        // The Steam UI may show different windows (commonly splash screens) as its main window so close the
        // current one and repeat until there are no more main windows or until timing out after 1 second.
        var mainUIProcessId = GetMainUIProcessId();
        if (mainUIProcessId is null)
        {
            _logger.LogError("Could not find main UI process for {LauncherName}.", LauncherName);
            return;
        }

        var isWindowVisible = true;
        var timeout = _timeProvider.GetUtcNow().AddSeconds(1);
        while (isWindowVisible && _timeProvider.GetUtcNow() < timeout)
        {
            var process = Process.GetProcessById(mainUIProcessId.Value);

            if (process.MainWindowHandle == 0)
            {
                isWindowVisible = false;
                continue;
            }

            _logger.LogInformation("Closing current main window with title '{WindowTitle}' for process {ProcessName} ({ProcessId}).",
                process.MainWindowTitle,
                process.ProcessName,
                process.Id);

            process.CloseMainWindow();
        }
    }

    private static int? GetMainUIProcessId()
    {
        // Look for the main Steam UI process by searching for the sole steamwebhelper process where its parent process
        // is the steam process.
        var launcherProcess = ProcessHelper.GetSessionProcessesByName(LauncherProcessName).FirstOrDefault();
        if (launcherProcess is null)
            return null;

        return ProcessHelper.GetSessionProcessesByName(WebHelperProcessName)
            .Where(x => x.GetParentProcessId() == launcherProcess.Id)
            .FirstOrDefault()
            ?.Id;
    }
}
