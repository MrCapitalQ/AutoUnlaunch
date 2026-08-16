using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using System.Diagnostics;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal class GogLauncherWatchingHandler(IProcessWatcher processWatcher,
    GogSettingsService gogSettingsService,
    ProcessWindowService processWindowService,
    ILogger<GogLauncherWatchingHandler> logger)
    : LauncherWatchingHandler(processWatcher, gogSettingsService, logger)
{
    private const string LauncherProcessName = "GalaxyClient";
    private const string RegistryRootPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\GOG.com\GalaxyClient";

    private readonly GogSettingsService _gogSettingsService = gogSettingsService;
    private readonly ProcessWindowService _processWindowService = processWindowService;
    private readonly ILogger<GogLauncherWatchingHandler> _logger = logger;

    public override string LauncherName => "GOG Galaxy";

    protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
    {
        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        return Task.FromResult(launcherProcessesResult.Items.Any());
    }

    protected override bool IsLauncherActivity(ProcessInfo processInfo)
    {
        var processPath = !string.IsNullOrWhiteSpace(processInfo.ProcessPath)
            ? Path.GetFullPath(processInfo.ProcessPath)
            : null;
        return !string.IsNullOrEmpty(processPath)
            && GetGameInstallPaths().Any(x => processPath.StartsWith(Path.GetFullPath(x), StringComparison.OrdinalIgnoreCase));
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

    private IEnumerable<string> GetGameInstallPaths()
    {
        using var gamesSubKey = Registry.LocalMachine.OpenSubKey($@"SOFTWARE\WOW6432Node\GOG.com\Games");
        if (gamesSubKey is null)
        {
            _logger.LogWarning("Could not open GOG games registry sub key.");
            yield break;
        }

        foreach (var gameSubKeyName in gamesSubKey.GetSubKeyNames())
        {
            using var gameSubKey = gamesSubKey.OpenSubKey(gameSubKeyName);
            if (gameSubKey is null)
            {
                _logger.LogWarning("Could not open GOG game registry sub key {GogGameSubKeyName}.", gameSubKeyName);
                continue;
            }

            var path = gameSubKey.GetValue("path")?.ToString();
            if (string.IsNullOrEmpty(path))
            {
                _logger.LogWarning("GOG game registry sub key {GogGameSubKeyName} does not have an entry for 'path'.",
                    gameSubKeyName);
                continue;
            }

            _logger.LogDebug("Found GOG game {GogGameSubKeyName} installed at {GogGameInstallPath}.",
                gameSubKeyName,
                path);
            yield return path;
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
