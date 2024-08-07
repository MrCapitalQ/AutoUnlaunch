﻿using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using MrCapitalQ.AutoUnlaunch.Launchers.Handlers;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal class EALauncherHandler(TimeProvider timeProvider,
    EASettingsService eaSettingsService,
    LauncherChildProcessChecker childProcessChecker,
    ILogger<EALauncherHandler> logger)
    : LauncherHandler(eaSettingsService, timeProvider, logger)
{
    private const string LauncherProcessName = "EADesktop";
    private static readonly IReadOnlySet<string> s_excludedProcessNames = new HashSet<string>
    {
        "EABackgroundService",
        "EACefSubProcess",
        "EAConnect_microsoft",
        "EACrashReporter",
        "EADesktop",
        "EAEgsProxy",
        "EAGEP",
        "EALauncher",
        "EALaunchHelper",
        "EALocalHostSvc",
        "EASteamProxy",
        "EAUninstall",
        "EAUpdater",
        "ErrorReporter",
        "GetGameToken32",
        "GetGameToken64",
        "IGOProxy32",
        "Link2EA",
        "OriginLegacyCompatibility"
    };

    private readonly EASettingsService _eaSettingsService = eaSettingsService;
    private readonly LauncherChildProcessChecker _childProcessChecker = childProcessChecker;

    protected override string LauncherName => "EA";

    protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
    {
        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        return Task.FromResult(launcherProcessesResult.Items.Any());
    }

    protected override Task<bool> IsLauncherActivityRunningAsync(CancellationToken cancellationToken)
        => Task.FromResult(_childProcessChecker.IsChildProcessRunning(LauncherProcessName, s_excludedProcessNames));

    protected override Task StopLauncherAsync(CancellationToken cancellationToken)
    {
        var stopMethod = _eaSettingsService.GetLauncherStopMethod();
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
                // Closing EA's main window will gracefully close out the whole launcher even when the main window is
                // not visible. Close the main window for matching processes with a main window until there are no more
                // or timing out after 1 second.
                var timeout = _timeProvider.GetUtcNow().AddSeconds(1);
                while (_timeProvider.GetUtcNow() < timeout)
                {
                    using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
                    var processes = launcherProcessesResult.Items
                        .Where(x => x.MainWindowHandle != 0)
                        .ToList();

                    if (processes.Count == 0)
                        return Task.CompletedTask;

                    foreach (var process in processes)
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

        return Task.CompletedTask;
    }

    protected override Task OnLauncherActivityEnded(CancellationToken cancellationToken)
    {
        if (_eaSettingsService.GetMinimizesOnActivityEnd() != true)
            return Task.CompletedTask;

        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        foreach (var process in launcherProcessesResult.Items)
        {
            _logger.LogInformation("Minimizing current main window with title '{WindowTitle}' ({WindowHandle}) for process {ProcessName} ({ProcessId}).",
                process.MainWindowTitle,
                process.MainWindowHandle,
                process.ProcessName,
                process.Id);

            // To force the main window into a minimize state without the animation, first hide the window then
            // reactivate in minimize state.
            process.MainWindowHandle.HideWindow();
            process.MainWindowHandle.MinimizeWindow(true);
        }

        return Task.CompletedTask;
    }
}
