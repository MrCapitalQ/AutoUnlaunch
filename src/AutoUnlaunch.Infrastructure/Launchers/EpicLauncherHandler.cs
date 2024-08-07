﻿using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal class EpicLauncherHandler(TimeProvider timeProvider,
    EpicSettingsService epicSettingsService,
    LauncherChildProcessChecker childProcessChecker,
    IProtocolLauncher protocolLauncher,
    ILogger<EpicLauncherHandler> logger)
    : LauncherHandler(epicSettingsService, timeProvider, logger)
{
    private const string LauncherProcessName = "EpicGamesLauncher";
    private const string UserHelperProcessName = "EpicOnlineServicesUserHelper";
    private static readonly Uri s_launchUri = new(LauncherUriProtocols.Epic);
    private static readonly IReadOnlySet<string> s_excludedProcessNames = new HashSet<string>
    {
        "CrashReportClient",
        "EOSBootStrapper",
        "EOSOverlayRenderer-Win32-Shipping",
        "EOSOverlayRenderer-Win64-Shipping",
        "EpicGamesLauncher",
        "EpicOnlineServices",
        "EpicOnlineServicesInstallHelper",
        "EpicOnlineServicesUIHelper",
        "EpicOnlineServicesUserHelper",
        "EpicOnlineServicesHost",
        "EpicWebHelper",
        "InstallChainer",
        "LauncherPrereqSetup_x64",
        "UnrealEngineLauncher",
        "UnrealVersionSelector"
    };

    private readonly EpicSettingsService _epicSettingsService = epicSettingsService;
    private readonly LauncherChildProcessChecker _childProcessChecker = childProcessChecker;
    private readonly IProtocolLauncher _protocolLauncher = protocolLauncher;

    protected override string LauncherName => "Epic Games";

    protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
    {
        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        return Task.FromResult(launcherProcessesResult.Items.Any());
    }

    protected override Task<bool> IsLauncherActivityRunningAsync(CancellationToken cancellationToken)
    {
        // Some game processes are not attached to the launcher process as a child but will spawn a user helper process
        // for Epic online services identity. If this is running, assume an Epic game is running. Otherwise, check for
        // processes spawned by the launcher process as usual.
        using var userHelperProcessesResult = ProcessHelper.GetSessionProcessesByName(UserHelperProcessName);
        if (userHelperProcessesResult.Items.Any())
            return Task.FromResult(true);

        return Task.FromResult(_childProcessChecker.IsChildProcessRunning(LauncherProcessName, s_excludedProcessNames));
    }

    protected override async Task StopLauncherAsync(CancellationToken cancellationToken)
    {
        var stopMethod = _epicSettingsService.GetLauncherStopMethod();
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
            case LauncherStopMethod.CloseMainWindow:
                // Closing Epic's main window will also gracefully close out the whole launcher. First launch it to
                // ensure there's a main window to close.
                await _protocolLauncher.LaunchUriAsync(s_launchUri);

                // Find and close any main windows that appear in the next 1 second.
                var timeout = _timeProvider.GetUtcNow().AddSeconds(1);
                while (DateTimeOffset.UtcNow < timeout)
                {
                    using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
                    var processesWithMainWindow = launcherProcessesResult.Items
                            .Where(x => x.MainWindowHandle != 0)
                            .ToList();

                    if (processesWithMainWindow.Count == 0)
                    {
                        await Task.Delay(50, cancellationToken);
                        continue;
                    }

                    foreach (var process in processesWithMainWindow)
                    {
                        _logger.LogInformation("Closing current main window with title '{WindowTitle}' ({WindowHandle}) for process {ProcessName} ({ProcessId}).",
                            process.MainWindowTitle,
                            process.MainWindowHandle,
                            process.ProcessName,
                            process.Id);

                        process.CloseMainWindow();
                    }
                }
                break;
            default:
                _logger.LogError("Stop method {StopMethod} is not supported for {LauncherName}.",
                    stopMethod,
                    LauncherName);
                break;
        }
    }
}
