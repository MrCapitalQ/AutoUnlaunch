using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using MrCapitalQ.AutoUnlaunch.Launchers.Handlers;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal partial class EALauncherProcessTrackingHandler(IProcessWatcher processWatcher,
    EASettingsService eaSettingsService,
    TimeProvider timeProvider,
    RegistryWatcherFactory registryWatcherFactory,
    ILogger<EALauncherProcessTrackingHandler> logger)
    : LauncherProcessTrackingHandler(processWatcher, eaSettingsService, timeProvider, logger), IAsyncDisposable
{
    private const string LauncherProcessName = "EADesktop";
    private const string RegistryRootPath = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";

    private static readonly RegistryHive s_registryHive = RegistryHive.LocalMachine;
    private static readonly HashSet<RegistryView> s_registryViews = [RegistryView.Registry32, RegistryView.Registry64];

    private readonly EASettingsService _eaSettingsService = eaSettingsService;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly RegistryWatcher _registry32Watcher = registryWatcherFactory.Create(s_registryHive,
        RegistryRootPath,
        RegistryView.Registry32);
    private readonly RegistryWatcher _registry64Watcher = registryWatcherFactory.Create(s_registryHive,
        RegistryRootPath,
        RegistryView.Registry64);
    private readonly ILogger<EALauncherProcessTrackingHandler> _logger = logger;
    private readonly SemaphoreSlim _lock = new(1);
    private readonly HashSet<string> _installPaths = new(StringComparer.OrdinalIgnoreCase);

    public override string LauncherName => "EA";

    protected override async Task StartCoreAsync(CancellationToken cancellationToken = default)
    {
        _registry32Watcher.Changed += RegistryWatcher_Changed;
        _registry32Watcher.Errored += RegistryWatcher_Errored;
        _registry64Watcher.Changed += RegistryWatcher_Changed;
        _registry64Watcher.Errored += RegistryWatcher_Errored;

        try
        {
            _registry32Watcher.Start();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start 32-bit view registry watcher. EA games list refresh will not function properly until handler is restarted.");
        }

        try
        {
            _registry64Watcher.Start();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start 64-bit view registry watcher. EA games list refresh will not function properly until handler is restarted.");
        }

        await UpdateInstallPathsAsync(cancellationToken);
    }

    protected override Task StopCoreAsync(CancellationToken cancellationToken = default)
    {
        _registry32Watcher.Changed -= RegistryWatcher_Changed;
        _registry32Watcher.Errored -= RegistryWatcher_Errored;
        _registry64Watcher.Changed -= RegistryWatcher_Changed;
        _registry64Watcher.Errored -= RegistryWatcher_Errored;

        _registry32Watcher.Stop();
        _registry64Watcher.Stop();

        return Task.CompletedTask;
    }

    protected override Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken)
    {
        using var launcherProcessesResult = ProcessHelper.GetSessionProcessesByName(LauncherProcessName);
        return Task.FromResult(launcherProcessesResult.Items.Any());
    }

    protected override async Task<bool> IsLauncherActivityAsync(ProcessInfo processInfo)
    {
        var processPath = !string.IsNullOrWhiteSpace(processInfo.ProcessPath)
            ? Path.GetFullPath(processInfo.ProcessPath)
            : null;

        if (string.IsNullOrEmpty(processPath))
            return false;

        await _lock.WaitAsync();

        try
        {
            return _installPaths.Any(x => processPath.StartsWith(Path.GetFullPath(x), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _lock.Release();
        }
    }

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
                        _logger.LogKillingProcess(process.ProcessName, process.Id);
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
                        _logger.LogClosingProcessMainWindow(process.MainWindowTitle,
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
            LogMinimizingProcessMainWindow(process.MainWindowTitle,
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

    private async Task UpdateInstallPathsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        _logger.LogInformation("Updating EA game install locations.");

        _installPaths.Clear();

        foreach (var view in s_registryViews)
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(s_registryHive, view);
                using var uninstallSubKey = baseKey.OpenSubKey(RegistryRootPath);
                if (uninstallSubKey is null)
                {
                    _logger.LogWarning("Could not open Windows application uninstall registry sub key.");
                    return;
                }

                foreach (var uninstallItemSubKeyName in uninstallSubKey.GetSubKeyNames())
                {
                    try
                    {
                        using var uninstallItemSubKey = uninstallSubKey.OpenSubKey(uninstallItemSubKeyName);
                        if (uninstallItemSubKey is null)
                        {
                            _logger.LogWarning("Could not open Windows application uninstall item registry sub key '{RegistrySubKeyName}'.",
                                uninstallItemSubKeyName);
                            continue;
                        }

                        var displayName = uninstallItemSubKey.GetValue("DisplayName")?.ToString();
                        var installLocation = uninstallItemSubKey.GetValue("InstallLocation")?.ToString();

                        if (string.IsNullOrEmpty(displayName) || string.IsNullOrEmpty(installLocation))
                        {
                            LogSkippingRegistryKeyWithoutDisplayNameOrInstallLocation(uninstallItemSubKeyName);
                            continue;
                        }

                        if (_installPaths.Contains(installLocation))
                        {
                            LogSkippingRegistryKeyAlreadyTracked(uninstallItemSubKeyName, installLocation);
                            continue;
                        }
                        else
                            LogFoundInstalledApplication(displayName, installLocation);

                        var installerDirectoryPath = Path.Combine(installLocation, "__Installer");
                        if (!Directory.Exists(installerDirectoryPath))
                        {
                            LogSkippingApplicationWithoutInstallerDirectory(displayName, installLocation);
                            continue;
                        }

                        if (!File.Exists(Path.Combine(installerDirectoryPath, "installerdata.xml")))
                        {
                            _logger.LogWarning("Skipping installed application '{ApplicationName}' because {ApplicationInstallerPath} does not contain a file named 'installerdata.xml'.",
                                displayName,
                                installerDirectoryPath);
                            continue;
                        }

                        try
                        {
                            var normalizedInstallPath = Path.GetFullPath(installLocation);
                            _installPaths.Add(normalizedInstallPath);
                            LogFoundEAGame(displayName, normalizedInstallPath);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex,
                                "Found EA game '{EAGameName}' but something went wrong while trying to track its install location of {EAGameInstallPath}.",
                                displayName,
                                installLocation);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Found Windows application uninstall item registry sub key '{RegistrySubKeyName}' but something went wrong while trying to read it.",
                            uninstallItemSubKeyName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong while updating EA game install paths. Game detection may not work properly.");
            }
            finally
            {
                _lock.Release();
            }
        }
    }

    private async void RegistryWatcher_Changed(object? sender, EventArgs e)
    {
        await UpdateInstallPathsAsync();
    }

    private void RegistryWatcher_Errored(object? sender, EventArgs e)
    {
        _logger.LogWarning("Registry watcher encountered an unexpected error and has stopped. EA games list refresh will not function properly until handler is restarted.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _registry32Watcher.Dispose();
        _registry64Watcher.Dispose();
        _lock.Dispose();
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Minimizing current main window with title '{WindowTitle}' ({WindowHandle}) for process {ProcessName} ({ProcessId}).")]
    public partial void LogMinimizingProcessMainWindow(string windowTitle,
        nint windowHandle,
        string processName,
        int processId);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping registry sub key '{RegistrySubKeyName}' because it does not have a display name or install location value.")]
    public partial void LogSkippingRegistryKeyWithoutDisplayNameOrInstallLocation(string registrySubKeyName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping registry sub key '{RegistrySubKeyName}' because its install location of {ApplicationInstallPath} is already being tracked.")]
    public partial void LogSkippingRegistryKeyAlreadyTracked(string registrySubKeyName, string applicationInstallPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Found installed application '{ApplicationName}' installed at {ApplicationInstallPath}.")]
    public partial void LogFoundInstalledApplication(string applicationName, string applicationInstallPath);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Skipping installed application '{ApplicationName}' because {ApplicationInstallPath} does not contain a directory named '__Installer'.")]
    public partial void LogSkippingApplicationWithoutInstallerDirectory(string applicationName, string applicationInstallPath);

    [LoggerMessage(Level = LogLevel.Information, Message = "Found EA game '{EAGameName}' installed at {EAGameInstallPath}.")]
    public partial void LogFoundEAGame(string eaGameName, string eaGameInstallPath);
}
