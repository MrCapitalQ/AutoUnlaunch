using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal class GogLauncherHandler : LauncherHandler
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

    private readonly GogSettingsService _gogSettingsService;
    private readonly LauncherChildProcessChecker _childProcessChecker;

    public GogLauncherHandler(TimeProvider timeProvider,
        GogSettingsService gogSettingsService,
        LauncherChildProcessChecker childProcessChecker,
        ILogger<GogLauncherHandler> logger)
        : base(gogSettingsService, timeProvider, logger)
    {
        _gogSettingsService = gogSettingsService;
        _childProcessChecker = childProcessChecker;
    }

    protected override string LauncherName => "GOG Galaxy";

    protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
        => Task.FromResult(ProcessHelper.GetSessionProcessesByName(LauncherProcessName).Any());

    protected override Task<bool> IsLauncherActivityRunningAsync(CancellationToken cancellationToken)
        => Task.FromResult(_childProcessChecker.IsChildProcessRunning(LauncherProcessName, s_excludedProcessNames));

    protected override async Task StopLauncherAsync(CancellationToken cancellationToken)
    {
        var stopMethod = _gogSettingsService.GetLauncherStopMethod();
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
                await RequestLauncherShutdown(cancellationToken);
                break;
            default:
                _logger.LogError("Stop method {StopMethod} is not supported for {LauncherName}.",
                    stopMethod,
                    LauncherName);
                break;
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
