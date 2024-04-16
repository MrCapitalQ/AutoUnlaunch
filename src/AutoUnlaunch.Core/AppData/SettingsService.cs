﻿namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

internal class SettingsService : ISettingsService
{
    private const string HasBeenLaunchedOnceSettingsKey = "HasBeenLaunchedOnce";
    private const string AppExitBehaviorSettingsKey = "AppExitBehavior";

    private readonly IApplicationDataStore _applicationDataStore;

    public SettingsService(IApplicationDataStore localApplicationData) => _applicationDataStore = localApplicationData;

    public bool GetHasBeenLaunchedOnce()
        => _applicationDataStore.GetValue(HasBeenLaunchedOnceSettingsKey) is bool value && value;

    public void SetHasBeenLaunchedOnce()
        => _applicationDataStore.SetValue(HasBeenLaunchedOnceSettingsKey, true);

    public AppExitBehavior GetAppExitBehavior()
    {
        if (_applicationDataStore.GetValue(AppExitBehaviorSettingsKey) is int value)
            return (AppExitBehavior)value;

        return AppExitBehavior.RunInBackground;
    }

    public void SetAppExitBehavior(AppExitBehavior appExitBehavior)
        => _applicationDataStore.SetValue(AppExitBehaviorSettingsKey, (int)appExitBehavior);
}
