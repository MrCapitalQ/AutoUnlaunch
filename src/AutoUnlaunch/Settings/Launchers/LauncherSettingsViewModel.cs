using CommunityToolkit.Mvvm.ComponentModel;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Shared;

namespace MrCapitalQ.AutoUnlaunch.Settings.Launchers;

internal abstract partial class LauncherSettingsViewModel : ObservableObject
{
    private static readonly List<ComboBoxOption<int>> s_delayOptions =
    [
        new(0, "No delay"),
        new(5, "5 second delay"),
        new(15, "15 second delay"),
        new(30, "30 second delay"),
        new(60, "60 second delay")
    ];
    protected static readonly Dictionary<LauncherStopMethod, string> s_stopMethodDisplayStrings = new()
    {
        { LauncherStopMethod.KillProcess, "Kill process" },
        { LauncherStopMethod.CloseMainWindow, "Close application" },
        { LauncherStopMethod.RequestShutdown, "Request shutdown" }
    };
    private readonly LauncherSettingsService _settingsService;

    [ObservableProperty]
    private bool _isEnabled;

    [ObservableProperty]
    private ComboBoxOption<int> _selectedDelay;

    [ObservableProperty]
    private ComboBoxOption<LauncherStopMethod> _selectedStopMethod;

    protected LauncherSettingsViewModel(LauncherSettingsService settingsService,
        LauncherStopMethod defaultStopMethod = LauncherStopMethod.KillProcess)
    {
        _settingsService = settingsService;

        IsEnabled = _settingsService.GetIsLauncherEnabled() ?? true;

        var selectedDelay = _settingsService.GetLauncherStopDelay() ?? 5;
        SelectedDelay = DelayOptions.Single(x => x.Value == selectedDelay);

        var selectedStopMethod = _settingsService.GetLauncherStopMethod() ?? defaultStopMethod;
        SelectedStopMethod = StopMethodOptions.Single(x => x.Value == selectedStopMethod);
    }

    public IEnumerable<ComboBoxOption<int>> DelayOptions => s_delayOptions;
    public abstract IEnumerable<ComboBoxOption<LauncherStopMethod>> StopMethodOptions { get; }

    partial void OnIsEnabledChanged(bool value) => _settingsService.SetIsLauncherEnabled(value);

    partial void OnSelectedDelayChanged(ComboBoxOption<int> value)
        => _settingsService.SetLauncherStopDelay(value.Value);

    partial void OnSelectedStopMethodChanged(ComboBoxOption<LauncherStopMethod> value)
        => _settingsService.SetLauncherStopMethod(value.Value);
}

