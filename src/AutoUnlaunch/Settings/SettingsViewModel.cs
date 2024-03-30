﻿using CommunityToolkit.Mvvm.ComponentModel;
using Windows.ApplicationModel;

namespace MrCapitalQ.AutoUnlaunch.Settings;

internal partial class SettingsViewModel : ObservableObject
{
    private readonly StartupTaskService _startupTaskService;

    private bool _isStartupOn;

    [ObservableProperty]
    private bool _isStartupToggleEnabled;

    [ObservableProperty]
    private string _startupSettingsText = string.Empty;

    public SettingsViewModel(StartupTaskService startupTaskService)
    {
        _startupTaskService = startupTaskService;
        UpdateStartupState();
    }

    public bool IsStartupOn
    {
        get => _isStartupOn;
        set => UpdateStartupState(value);
    }

    private async void UpdateStartupState(bool? isEnabled = null)
    {
        IsStartupToggleEnabled = false;

        if (isEnabled is not null)
            await _startupTaskService.SetStartupStateAsync(isEnabled.Value);

        var state = await _startupTaskService.GetStartupStateAsync();

        StartupSettingsText = "Start automatically in the background when you sign in";
        switch (state)
        {
            case StartupTaskState.DisabledByUser:
                StartupSettingsText = "Startup is disabled at the system level and must be enabled using the Startup tab in Task Manager";
                _isStartupOn = false;
                IsStartupToggleEnabled = false;
                break;
            case StartupTaskState.DisabledByPolicy:
                StartupSettingsText = "Startup is disabled by group policy or not supported on this device";
                _isStartupOn = false;
                IsStartupToggleEnabled = false;
                break;
            case StartupTaskState.Disabled:
                _isStartupOn = false;
                IsStartupToggleEnabled = true;
                break;
            case StartupTaskState.EnabledByPolicy:
                StartupSettingsText = "Startup is enabled by group policy";
                _isStartupOn = true;
                IsStartupToggleEnabled = false;
                break;
            case StartupTaskState.Enabled:
                _isStartupOn = true;
                IsStartupToggleEnabled = true;
                break;
        }

        OnPropertyChanged(nameof(IsStartupOn));
    }
}
