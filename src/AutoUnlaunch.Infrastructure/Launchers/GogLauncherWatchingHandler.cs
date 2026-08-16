using GameFinder.Common;
using GameFinder.RegistryUtils;
using GameFinder.StoreHandlers.GOG;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using NexusMods.Paths;
using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal class GogLauncherWatchingHandler : LauncherWatchingHandler
{
    private const string LauncherProcessName = "GalaxyClient";
    private const string RegistryRootPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient";

    private readonly GogSettingsService _gogSettingsService;
    private readonly ProcessWindowService _processWindowService;
    private readonly ILogger<GogLauncherWatchingHandler> _logger;
    private readonly GOGHandler _gogHandler;

    public GogLauncherWatchingHandler(ProcessWatcher processWatcher,
        GogSettingsService gogSettingsService,
        ProcessWindowService processWindowService,
        ILogger<GogLauncherWatchingHandler> logger) : base(processWatcher, gogSettingsService, logger)
    {
        _gogSettingsService = gogSettingsService;
        _processWindowService = processWindowService;
        _logger = logger;

        _gogHandler = new GOGHandler(WindowsRegistry.Shared, FileSystem.Shared);
    }

    protected override string LauncherName => "GOG Galaxy";

    protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
    {
        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        return Task.FromResult(launcherProcessesResult.Items.Any());
    }

    protected override bool IsLauncherActivity(ProcessInfo processInfo)
    {
        var games = _gogHandler.FindAllGames().Where(x => x.IsT0).Select(x => x.AsGame());
        var processPath = !string.IsNullOrWhiteSpace(processInfo.ProcessPath)
            ? Path.GetFullPath(processInfo.ProcessPath)
            : null;
        return !string.IsNullOrEmpty(processPath)
            && games.Any(x => processPath.StartsWith(Path.GetFullPath(x.Path.FileName), StringComparison.OrdinalIgnoreCase));
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
