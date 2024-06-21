using MrCapitalQ.AutoUnlaunch.Core.Logging;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace MrCapitalQ.AutoUnlaunch.Shared;

[ExcludeFromCodeCoverage(Justification = ExcludeFromCoverageJustifications.RequiresUIThread)]
internal class LogExporter : ILogExporter
{
    public async Task ExportLogsAsync()
    {
        var savePicker = new FileSavePicker();
        InitializeWithWindow.Initialize(savePicker, WindowNative.GetWindowHandle(App.Current.MainWindow));

        savePicker.SuggestedStartLocation = PickerLocationId.Desktop;
        savePicker.FileTypeChoices.Add("Compressed ZIP file", [".zip"]);
        savePicker.SuggestedFileName = $"AutoUnlaunchLogs_{DateTimeOffset.Now.Ticks}.zip";

        var saveFile = await savePicker.PickSaveFileAsync();
        if (saveFile is null)
            return;

        using var zipFile = new FileStream(saveFile.Path, FileMode.Create);
        using var archive = new ZipArchive(zipFile, ZipArchiveMode.Create);
        foreach (var file in Directory.GetFiles(Path.Combine(ApplicationData.Current.TemporaryFolder.Path, "Logs")))
        {
            var entry = archive.CreateEntry(Path.GetFileName(file));
            entry.LastWriteTime = File.GetLastWriteTime(file);

            using var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var stream = entry.Open();
            await fs.CopyToAsync(stream);
        }
    }
}
