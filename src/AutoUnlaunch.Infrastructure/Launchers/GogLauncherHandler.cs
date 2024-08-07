﻿using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal class GogLauncherHandler(TimeProvider timeProvider,
    GogSettingsService gogSettingsService,
    LauncherChildProcessChecker childProcessChecker,
    ProcessWindowService processWindowService,
    ILogger<GogLauncherHandler> logger)
    : LauncherHandler(gogSettingsService, timeProvider, logger)
{
    private const string LauncherProcessName = "GalaxyClient";
    private const string RegistryRootPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient";
    private static readonly IReadOnlySet<string> s_excludedProcessNames = new HashSet<string>
    {
        "CrashReporter",
        "GalaxyClient Helper",
        "GalaxyClient",
        "GalaxyClientService",
        "GOG Galaxy Notifications Renderer"
    };

    private readonly GogSettingsService _gogSettingsService = gogSettingsService;
    private readonly LauncherChildProcessChecker _childProcessChecker = childProcessChecker;
    private readonly ProcessWindowService _processWindowService = processWindowService;

    protected override string LauncherName => "GOG Galaxy";

    protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
    {
        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        return Task.FromResult(launcherProcessesResult.Items.Any());
    }

    protected override Task<bool> IsLauncherActivityRunningAsync(CancellationToken cancellationToken)
        => Task.FromResult(_childProcessChecker.IsChildProcessRunning(LauncherProcessName, s_excludedProcessNames));

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
                        _logger.LogInformation("Killing process {ProcessName} ({ProcessId}).",
                            process.ProcessName,
                            process.Id);
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
            _logger.LogInformation("Request to gracefully shutdown {LauncherName} succeeded.", LauncherName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Request to gracefully shutdown {LauncherName} failed.", LauncherName);
        }
    }
}
