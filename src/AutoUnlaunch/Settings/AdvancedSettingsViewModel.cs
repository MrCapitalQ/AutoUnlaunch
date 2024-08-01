using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Logging;
using MrCapitalQ.AutoUnlaunch.Shared;

namespace MrCapitalQ.AutoUnlaunch.Settings;

internal partial class AdvancedSettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settingsService;
    private readonly ILogLevelManager _logLevelManager;
    private readonly ILogExporter _logExporter;
    private readonly IMessenger _messenger;
    private readonly ILogger<AdvancedSettingsViewModel> _logger;

    [ObservableProperty]
    private ComboBoxOption<AppExitBehavior> _selectedExitBehavior;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsLoggingLevelWarningVisible))]
    private ComboBoxOption<LogLevel> _selectedLogLevel;

    [ObservableProperty]
    private bool _isExporting;

    public AdvancedSettingsViewModel(ISettingsService settingsService,
        ILogLevelManager logLevelManager,
        ILogExporter logExporter,
        IMessenger messenger,
        ILogger<AdvancedSettingsViewModel> logger)
    {
        _settingsService = settingsService;
        _logLevelManager = logLevelManager;
        _logExporter = logExporter;
        _messenger = messenger;
        _logger = logger;

        _selectedExitBehavior = ExitBehaviorOptions.FirstOrDefault(x => x.Value == _settingsService.GetAppExitBehavior())
            ?? ExitBehaviorOptions.First(x => x.Value == AppExitBehavior.RunInBackground);

        _selectedLogLevel = LogLevelOptions.FirstOrDefault(x => x.Value == _settingsService.GetMinimumLogLevel())
            ?? LogLevelOptions.First(x => x.Value == LogLevel.Information);
    }

    public List<ComboBoxOption<AppExitBehavior>> ExitBehaviorOptions { get; } =
    [
        new(AppExitBehavior.Ask, "Ask every time"),
        new(AppExitBehavior.RunInBackground, "Run in the background"),
        new(AppExitBehavior.Stop, "Stop application")
    ];

    public List<ComboBoxOption<LogLevel>> LogLevelOptions { get; } =
    [
        new(LogLevel.None, "None"),
        new(LogLevel.Information, "Default"),
        new(LogLevel.Debug, "Debug")
    ];

    public bool IsLoggingLevelWarningVisible => SelectedLogLevel.Value == LogLevel.Debug;

    [RelayCommand]
    private async Task ExportLogsAsync()
    {
        IsExporting = true;

        try
        {
            await _logExporter.ExportLogsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while exporting the application logs.");
            _messenger.Send(new ShowDialogMessage("Error", "Something went wrong while exporting the logs."));
        }
        finally
        {
            IsExporting = false;
        }
    }

    partial void OnSelectedExitBehaviorChanged(ComboBoxOption<AppExitBehavior> value)
        => _settingsService.SetAppExitBehavior(value.Value);

    partial void OnSelectedLogLevelChanged(ComboBoxOption<LogLevel> value)
    {
        _settingsService.SetMinimumLogLevel(value.Value);
        _logLevelManager.SetMinimumLogLevel(value.Value);
    }
}
