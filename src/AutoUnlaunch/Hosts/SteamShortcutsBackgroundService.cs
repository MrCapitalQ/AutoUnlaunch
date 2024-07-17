using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace MrCapitalQ.AutoUnlaunch.Hosts;

[ExcludeFromCodeCoverage]
internal partial class SteamShortcutsBackgroundService : BackgroundService
{
    private readonly string _programsShortcutDirectoryPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
    private readonly FileSystemWatcher _fileSystemWatcher = new();
    private readonly ILogger<SteamShortcutsBackgroundService> _logger;

    public SteamShortcutsBackgroundService(ILogger<SteamShortcutsBackgroundService> logger)
    {
        _logger = logger;

        _fileSystemWatcher.Created += FileSystemWatcher_Created;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var steamStartMenuPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Steam");

        foreach (var shortcutPath in Directory.GetFiles(steamStartMenuPath, "*.url"))
        {
            await TryHandleShortcutAsync(shortcutPath);
        }

        _fileSystemWatcher.Path = steamStartMenuPath;
        _fileSystemWatcher.Filter = "*.url";
        _fileSystemWatcher.EnableRaisingEvents = true;
    }

    private async Task TryHandleShortcutAsync(string shortcutPath)
    {
        _logger.LogTrace("Attempting to handle Steam shortcut {ShortcutPath}.", shortcutPath);

        string content;
        try
        {
            content = await File.ReadAllTextAsync(shortcutPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read file content of {ShortcutPath}.", shortcutPath);
            return;
        }

        var match = UrlShortcutTargetRegex().Match(content);
        if (!match.Success)
        {
            _logger.LogDebug("Shortcut handling is skipped because of an unexpected URL value in {ShortcutPath}.", shortcutPath);
            return;
        }

        var silentArgument = !string.IsNullOrWhiteSpace(match.Groups[1].Value);
        if (!silentArgument)
        {
            _logger.LogInformation("Appending silent launch argument for Steam shortcut {ShortcutPath}.", shortcutPath);

            content = UrlShortcutTargetRegex().Replace(content, m => m.Groups[0].Value + "\" -silent");
            await File.WriteAllTextAsync(shortcutPath, content);
        }

        var newShortcutPath = Path.Combine(_programsShortcutDirectoryPath, Path.GetFileName(shortcutPath));
        _logger.LogDebug("Moving Steam shortcut from {ShortcutPath} to {TargetPath}.", shortcutPath, newShortcutPath);

        try
        {
            var moveShortcutCommand = new Process
            {
                StartInfo = new ProcessStartInfo()
                {
                    FileName = "cmd.exe",
                    Arguments = $"/C move \"{shortcutPath}\" \"{newShortcutPath}\"",
                    CreateNoWindow = true
                }
            };
            moveShortcutCommand.Start();
            await moveShortcutCommand.WaitForExitAsync();

            _logger.LogInformation("Moved Steam shortcut from {ShortcutPath} to {TargetPath}.", shortcutPath, newShortcutPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to move steam shortcut {ShortcutPath} to {TargetPath}.", shortcutPath, newShortcutPath);
        }
    }

    private Task RestoreShortcutAsync(string shortcutPath)
    {
        throw new NotImplementedException();
    }

    private async void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
    {
        await TryHandleShortcutAsync(e.FullPath);
    }

    [GeneratedRegex(@"URL=steam://rungameid/\d+("" -silent)?")]
    private static partial Regex UrlShortcutTargetRegex();
}
