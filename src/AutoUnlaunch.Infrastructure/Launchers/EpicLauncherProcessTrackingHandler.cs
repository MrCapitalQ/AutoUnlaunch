using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

internal partial class EpicLauncherProcessTrackingHandler(IProcessWatcher processWatcher,
    EpicSettingsService epicSettingsService,
    TimeProvider timeProvider,
    IProtocolLauncher protocolLauncher,
    ILogger<EpicLauncherProcessTrackingHandler> logger)
    : LauncherProcessTrackingHandler(processWatcher, epicSettingsService, timeProvider, logger)
{
    private const string LauncherProcessName = "EpicGamesLauncher";
    private const string ManifestItemFilePattern = "*.item";
    private static readonly Uri s_launchUri = new(LauncherUriProtocols.Epic);

    private readonly EpicSettingsService _epicSettingsService = epicSettingsService;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly IProtocolLauncher _protocolLauncher = protocolLauncher;
    private readonly ILogger<EpicLauncherProcessTrackingHandler> _logger = logger;
    private readonly SemaphoreSlim _lock = new(1);
    private readonly Dictionary<string, string> _installPaths = [];

    private FileSystemWatcher? _fileSystemWatcher;

    public override string LauncherName => "Epic Games";

    protected override async Task StartCoreAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (GetManifestDirectoryPath() is not { Length: > 0 } manifestDirectoryPath)
            {
                _logger.LogWarning("Failed to start file system watcher because failed to get Epic manifest directory path. Epic games list will not be refreshed until handler is restarted.");
            }
            else
            {
                _fileSystemWatcher = new(manifestDirectoryPath, ManifestItemFilePattern);
                _fileSystemWatcher.Changed += FileSystemWatcher_Changed;
                _fileSystemWatcher.Created += FileSystemWatcher_Changed;
                _fileSystemWatcher.Deleted += FileSystemWatcher_Changed;
                _fileSystemWatcher.Renamed += FileSystemWatcher_Changed;
                _fileSystemWatcher.Error += FileSystemWatcher_Error;
                _fileSystemWatcher.EnableRaisingEvents = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to start file system watcher. Epic games list will not be refreshed until handler is restarted.");
        }
        await UpdateInstallPathsAsync(cancellationToken);
    }

    protected override Task StopCoreAsync(CancellationToken cancellationToken = default)
    {
        if (_fileSystemWatcher is not null)
        {
            _fileSystemWatcher.EnableRaisingEvents = false;
            _fileSystemWatcher.Changed -= FileSystemWatcher_Changed;
            _fileSystemWatcher.Created -= FileSystemWatcher_Changed;
            _fileSystemWatcher.Deleted -= FileSystemWatcher_Changed;
            _fileSystemWatcher.Renamed -= FileSystemWatcher_Changed;
            _fileSystemWatcher.Error -= FileSystemWatcher_Error;
            _fileSystemWatcher.Dispose();
            _fileSystemWatcher = null;
        }
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
            return _installPaths.Values.Any(x => processPath.StartsWith(Path.GetFullPath(x), StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            _lock.Release();
        }
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
                        _logger.LogKillingProcess(process.ProcessName, process.Id);
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
    }

    // Note that a registry watcher isn't set up to re-run this when the registry key for the manifest directory path
    // changes because change notifications do not get raised for the CURRENT_USER hive when registry write
    // virtualization is not disabled. https://github.com/microsoft/WindowsAppSDK/issues/4075
    //
    // Disabling the registry write virtualization will be considered for later if it can pass Windows Store
    // certification.
    private async Task UpdateInstallPathsAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);

        _logger.LogInformation("Updating Epic game install locations.");

        _installPaths.Clear();

        try
        {
            var manifestsDirectoryPath = GetManifestDirectoryPath();
            if (!Path.Exists(manifestsDirectoryPath))
            {
                _logger.LogWarning("Epic manifest directory does not exist at {EpicManifestDirectoryPath}.",
                    manifestsDirectoryPath);
                return;
            }

            foreach (var manifestPath in Directory.EnumerateFiles(manifestsDirectoryPath, ManifestItemFilePattern))
            {
                try
                {
                    using var stream = new FileStream(manifestPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    if (JsonSerializer.Deserialize(stream, EpicManifestServiceSerializerContext.Default.EpicItemManifest) is not { } manifest
                        || manifest.InstallLocation is not { Length: > 0 })
                    {
                        _logger.LogWarning("Skipping Epic game manifest at {EpicManifestPath} because it does not have an install location value.",
                            manifestPath);
                        continue;
                    }

                    try
                    {
                        var normalizedInstallPath = Path.GetFullPath(manifest.InstallLocation);
                        _installPaths[manifest.AppLaunchId] = normalizedInstallPath;
                        LogFoundEpicGame(manifest.DisplayName, manifest.AppLaunchId, normalizedInstallPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Found Epic game '{EpicGameName}' ({EpicGameAppLaunchId}) but something went wrong while trying to track its install location of {EpicGameInstallPath}.",
                            manifest.DisplayName,
                            manifest.AppLaunchId,
                            manifest.InstallLocation);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex,
                        "Found Epic game manifest at {EpicManifestPath} but something went wrong while trying to read it.",
                        manifestPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Something went wrong while updating Epic game install paths. Game detection may not work properly.");
        }
        finally
        {
            _lock.Release();
        }
    }

    private string? GetManifestDirectoryPath()
    {
        try
        {
            using var baseKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            using var eosKey = baseKey.OpenSubKey(@"Software\Epic Games\EOS");

            var manifestsDirectoryPath = eosKey?.GetValue("ModSdkMetadataDir")?.ToString();
            if (!string.IsNullOrEmpty(manifestsDirectoryPath))
                return Path.GetFullPath(manifestsDirectoryPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not get Epic manifest directory path.");
            return null;
        }

        _logger.LogWarning("Could not get Epic manifest directory path.");
        return null;
    }

    private async void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e)
    {
        await UpdateInstallPathsAsync();
    }

    private void FileSystemWatcher_Error(object sender, ErrorEventArgs e)
    {
        var ex = e.GetException();
        _logger.LogWarning(ex,
            "File system watcher encountered an unexpected error and has stopped. Epic games list will not be refreshed until handler is restarted.");
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Found Epic game '{EpicGameName}' ({EpicGameAppLaunchId}) installed at {EpicGameInstallPath}.")]
    public partial void LogFoundEpicGame(string epicGameName, string epicGameAppLaunchId, string epicGameInstallPath);

    [JsonSourceGenerationOptions(PropertyNameCaseInsensitive = true)]
    [JsonSerializable(typeof(EpicItemManifest))]
    private partial class EpicManifestServiceSerializerContext : JsonSerializerContext { }

    private record EpicItemManifest
    {
        public required string DisplayName { get; init; }
        public required string CatalogNamespace { get; init; }
        public required string CatalogItemId { get; init; }
        public required string AppName { get; init; }
        public string? InstallLocation { get; init; }
        public string AppLaunchId => $"{CatalogNamespace}:{CatalogItemId}:{AppName}";
    }
}