using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal class GogLauncherWatchingHandler(ProcessWatcher processWatcher,
    GogSettingsService gogSettingsService,
    GogGalaxyLibrary gogGalaxyLibrary,
    ProcessWindowService processWindowService,
    ILogger<GogLauncherWatchingHandler> logger)
    : LauncherWatchingHandler(processWatcher, gogSettingsService, logger)
{
    private const string LauncherProcessName = "GalaxyClient";
    private const string RegistryRootPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient";

    private readonly GogSettingsService _gogSettingsService = gogSettingsService;
    private readonly GogGalaxyLibrary _gogGalaxyLibrary = gogGalaxyLibrary;
    private readonly ProcessWindowService _processWindowService = processWindowService;
    private readonly ILogger<GogLauncherWatchingHandler> _logger = logger;

    protected override string LauncherName => "GOG Galaxy";

    protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
    {
        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        return Task.FromResult(launcherProcessesResult.Items.Any());
    }

    protected override bool IsLauncherActivity(ProcessInfo processInfo)
    {
        var games = _gogGalaxyLibrary.GetForCurrentUser();
        var processPath = !string.IsNullOrWhiteSpace(processInfo.ProcessPath)
            ? Path.GetFullPath(processInfo.ProcessPath)
            : null;
        return !string.IsNullOrEmpty(processPath)
            && games.Any(x => processPath.StartsWith(Path.GetFullPath(x.InstallPath), StringComparison.OrdinalIgnoreCase));
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
