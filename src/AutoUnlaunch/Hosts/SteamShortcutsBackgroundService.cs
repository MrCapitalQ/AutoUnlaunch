using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace MrCapitalQ.AutoUnlaunch.Hosts;

[ExcludeFromCodeCoverage]
internal partial class SteamShortcutsBackgroundService : BackgroundService
{
    private readonly string _programsShortcutsDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
    private readonly string _steamStartMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Steam");
    private readonly FileSystemWatcher _fileSystemWatcher = new();
    private readonly SteamSettingsService _steamSettingsService;
    private readonly ILogger<SteamShortcutsBackgroundService> _logger;

    public SteamShortcutsBackgroundService(SteamSettingsService steamSettingsService,
        IMessenger messenger,
        ILogger<SteamShortcutsBackgroundService> logger)
    {
        _steamSettingsService = steamSettingsService;
        _logger = logger;

        _fileSystemWatcher.Created += FileSystemWatcher_Created;
        _fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;

        messenger.Register<SteamShortcutsBackgroundService, SteamStartMenuSettingsChangedMessage>(this,
            async (r, m) => await r.ExecuteAsync(CancellationToken.None));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            if (_steamSettingsService.GetShowUnnestedInStartMenu() is not true)
            {
                _logger.LogInformation("Steam shortcuts handling is disabled. Ensuring shortcuts are restored to default state.");

                _fileSystemWatcher.EnableRaisingEvents = false;
                await RestoreShortcutsAsync();
                return;
            }

            _logger.LogInformation("Steam shortcuts handling is enabled. Ensuring shortcuts are in the correct state.");

            foreach (var shortcutPath in Directory.GetFiles(_steamStartMenuPath, "*.url"))
            {
                await TryHandleShortcutAsync(shortcutPath);
            }

            await CleanupShortcutsAsync();

            _fileSystemWatcher.Path = _steamStartMenuPath;
            _fileSystemWatcher.Filter = "*.url";
            _fileSystemWatcher.EnableRaisingEvents = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when executing the Steam shortcuts background service.");
        }
    }

    private async Task TryHandleShortcutAsync(string shortcutPath)
    {
        _logger.LogTrace("Attempting to handle Steam shortcut {ShortcutPath}.", shortcutPath);

        try
        {
            if (File.GetAttributes(shortcutPath).HasFlag(FileAttributes.Hidden))
            {
                _logger.LogDebug("Shortcut is hidden. Skipping {ShortcutPath}.", shortcutPath);
                return;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get file attributes of {ShortcutPath}.", shortcutPath);
            return;
        }

        if (string.IsNullOrEmpty(await GetSteamShortcutUrlAsync(shortcutPath)))
        {
            _logger.LogDebug("Shortcut does not appear to be a Steam shortcut. Skipping {ShortcutPath}.",
                shortcutPath);
            return;
        }

        var newShortcutPath = Path.Combine(_programsShortcutsDirectoryPath, Path.GetFileName(shortcutPath));
        _logger.LogDebug("Copying Steam shortcut from {ShortcutPath} to {TargetPath}.", shortcutPath, newShortcutPath);

        try
        {
            // Executing a copy command because File.Copy() doesn't work properly inside of these directories.
            var copyShortcutCommand = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C COPY \"{shortcutPath}\" \"{newShortcutPath}\"",
                    CreateNoWindow = true
                }
            };
            copyShortcutCommand.Start();
            await copyShortcutCommand.WaitForExitAsync();

            if (!File.Exists(newShortcutPath))
                _logger.LogError("Failed to copy Steam shortcut {ShortcutPath} to {TargetPath}.",
                    shortcutPath,
                    newShortcutPath);
            else
                _logger.LogInformation("Copied Steam shortcut from {ShortcutPath} to {TargetPath}.",
                    shortcutPath,
                    newShortcutPath);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy Steam shortcut {ShortcutPath} to {TargetPath}.",
                shortcutPath,
                newShortcutPath);
        }

        try
        {
            File.SetAttributes(shortcutPath, File.GetAttributes(shortcutPath) | FileAttributes.Hidden);
            _logger.LogInformation("Added hidden attribute to original Steam shortcut {ShortcutPath}.", shortcutPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add hidden attribute to original Steam shortcut {ShortcutPath}.",
                shortcutPath);
        }
    }

    private async Task RestoreShortcutsAsync()
    {
        _logger.LogTrace("Restoring Steam Start menu entries to the Steam folder.");

        foreach (var shortcutPath in Directory.GetFiles(_programsShortcutsDirectoryPath, "*.url"))
        {
            if (string.IsNullOrEmpty(await GetSteamShortcutUrlAsync(shortcutPath)))
            {
                _logger.LogDebug("Shortcut does not appear to be a Steam shortcut. Skipping {ShortcutPath}.",
                    shortcutPath);
                continue;
            }

            try
            {
                File.Delete(shortcutPath);
                _logger.LogInformation("Deleted shortcut {ShortcutPath}.", shortcutPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete shortcut {ShortcutPath}.", shortcutPath);
            }
        }

        foreach (var shortcutPath in Directory.GetFiles(_steamStartMenuPath, "*.url"))
        {
            if (string.IsNullOrEmpty(await GetSteamShortcutUrlAsync(shortcutPath)))
            {
                _logger.LogDebug("Shortcut does not appear to be a Steam shortcut. Skipping {ShortcutPath}.",
                    shortcutPath);
                continue;
            }

            try
            {
                File.SetAttributes(shortcutPath, File.GetAttributes(shortcutPath) & ~FileAttributes.Hidden);
                _logger.LogInformation("Removed hidden attribute from original Steam shortcut {ShortcutPath}.",
                    shortcutPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove hidden attribute from original Steam shortcut {ShortcutPath}.",
                    shortcutPath);
            }
        }

        _logger.LogInformation("Restored Steam Start menu entries to the Steam folder.");
    }

    private async Task CleanupShortcutsAsync()
    {
        var remainingOriginalShortcutUrls = (await Task.WhenAll(Directory.GetFiles(_steamStartMenuPath, "*.url")
            .Select(GetSteamShortcutUrlAsync)))
            .OfType<string>()
            .ToHashSet();

        foreach (var shortcutPath in Directory.GetFiles(_programsShortcutsDirectoryPath, "*.url"))
        {
            if (await GetSteamShortcutUrlAsync(shortcutPath) is not { } shortcutUrl
                || remainingOriginalShortcutUrls.Contains(shortcutUrl))
                continue;

            try
            {
                File.Delete(shortcutPath);
                _logger.LogInformation("Original Steam shortcut with matching URL {ShortcutUrl} not found. Deleted shortcut {ShortcutPath}.",
                    shortcutUrl,
                    shortcutPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete shortcut {ShortcutPath}.", shortcutPath);
            }
        }
    }

    private async Task<string?> GetSteamShortcutUrlAsync(string shortcutPath)
    {
        for (var currentAttempt = 0; currentAttempt < 5; currentAttempt++)
        {
            _logger.LogDebug("Attempt #{CurrentAttempt} to get the URL value of {ShortcutPath}.",
                currentAttempt + 1,
                shortcutPath);

            try
            {
                using var fileStream = new FileStream(shortcutPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var streamReader = new StreamReader(fileStream);
                var content = streamReader.ReadToEnd();

                var match = UrlShortcutTargetRegex().Match(content);
                if (!match.Success)
                {
                    _logger.LogDebug("Content does not contain an expected URL value for a Steam shortcut for {ShortcutPath}.\n{ShortcutContent}",
                        shortcutPath,
                        content);

                    // Continue to retry reading the file content because the file may not be fully written.
                    continue;
                }

                return match.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read file content of {ShortcutPath}.", shortcutPath);
            }

            await Task.Delay(100);
        }

        return null;
    }

    private async void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
    {
        // Wait 1 second to increase the odds nothing is still writing to it.
        await Task.Delay(1000);

        try
        {
            await TryHandleShortcutAsync(e.FullPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when trying to handle a newly created shortcut.");
        }
    }

    private async void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
    {
        try
        {
            await CleanupShortcutsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred when attempting clean up shortcuts after a shortcut has been deleted.");
        }
    }

    [GeneratedRegex(@"URL=steam://rungameid/\d+")]
    private static partial Regex UrlShortcutTargetRegex();
}
