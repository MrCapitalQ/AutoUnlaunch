using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Helpers;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Startup;
using MrCapitalQ.AutoUnlaunch.Settings.Launchers.EA;
using MrCapitalQ.AutoUnlaunch.Settings.Launchers.Gog;
using MrCapitalQ.AutoUnlaunch.Settings.Launchers.Steam;
using MrCapitalQ.AutoUnlaunch.Shared;

namespace MrCapitalQ.AutoUnlaunch.Settings;

internal partial class SettingsViewModel : ObservableObject
{
    private readonly IStartupTaskService _startupTaskService;
    private readonly ISettingsService _settingsService;

    private bool _isStartupOn;

    [ObservableProperty]
    private bool _isStartupToggleEnabled;

    [ObservableProperty]
    private string _startupSettingsText = string.Empty;

    [ObservableProperty]
    private ComboBoxOption<AppExitBehavior> _selectedExitBehavior;

    public SettingsViewModel(IStartupTaskService startupTaskService,
        ISettingsService settingsService,
        IPackageInfo packageInfo,
        ISteamSettingsViewModel steamSettingsViewModel,
        IEASettingsViewModel eaSettingsViewModel,
        IGogSettingsViewModel gogSettingsViewModel)
    {
        _startupTaskService = startupTaskService;
        _settingsService = settingsService;
        SteamSettings = steamSettingsViewModel;
        EASettings = eaSettingsViewModel;
        GogSettings = gogSettingsViewModel;

        UpdateStartupState();
        SelectedExitBehavior = ExitBehaviorOptions.FirstOrDefault(x => x.Value == _settingsService.GetAppExitBehavior())
            ?? ExitBehaviorOptions.First(x => x.Value == AppExitBehavior.RunInBackground);
        AppDisplayName = packageInfo.DisplayName;
        Version = packageInfo.Version.ToFormattedString(3);
    }

    public bool IsStartupOn
    {
        get => _isStartupOn;
        set => UpdateStartupState(value);
    }

    public List<ComboBoxOption<AppExitBehavior>> ExitBehaviorOptions { get; } =
    [
        new(AppExitBehavior.Ask, "Ask everytime"),
        new(AppExitBehavior.RunInBackground, "Run in the background"),
        new(AppExitBehavior.Stop, "Stop application")
    ];

    public string AppDisplayName { get; }
    public string Version { get; }

    public ISteamSettingsViewModel SteamSettings { get; }
    public IEASettingsViewModel EASettings { get; }
    public IGogSettingsViewModel GogSettings { get; }

    private async void UpdateStartupState(bool? isEnabled = null)
    {
        IsStartupToggleEnabled = false;

        if (isEnabled is not null)
            await _startupTaskService.SetStartupStateAsync(isEnabled.Value);

        var state = await _startupTaskService.GetStartupStateAsync();

        StartupSettingsText = "Start automatically in the background when you sign in";
        switch (state)
        {
            case AppStartupState.DisabledByUser:
                StartupSettingsText = "Startup is disabled at the system level and must be enabled using the Startup tab in Task Manager";
                _isStartupOn = false;
                IsStartupToggleEnabled = false;
                break;
            case AppStartupState.DisabledByPolicy:
                StartupSettingsText = "Startup is disabled by group policy or not supported on this device";
                _isStartupOn = false;
                IsStartupToggleEnabled = false;
                break;
            case AppStartupState.Disabled:
                _isStartupOn = false;
                IsStartupToggleEnabled = true;
                break;
            case AppStartupState.EnabledByPolicy:
                StartupSettingsText = "Startup is enabled by group policy";
                _isStartupOn = true;
                IsStartupToggleEnabled = false;
                break;
            case AppStartupState.Enabled:
                _isStartupOn = true;
                IsStartupToggleEnabled = true;
                break;
        }

        OnPropertyChanged(nameof(IsStartupOn));
    }

    partial void OnSelectedExitBehaviorChanged(ComboBoxOption<AppExitBehavior> value)
        => _settingsService.SetAppExitBehavior(value.Value);
}
