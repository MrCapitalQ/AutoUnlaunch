using MrCapitalQ.AutoUnlaunch.Shared;

namespace MrCapitalQ.AutoUnlaunch.Settings;

public class SettingsService
{
    private const string AppExitBehaviorSettingsKey = "AppExitBehavior";

    private readonly LocalApplicationData _localApplicationData;

    public SettingsService(LocalApplicationData localApplicationData) => _localApplicationData = localApplicationData;

    public AppExitBehavior GetAppExitBehavior()
    {
        if (_localApplicationData.GetValue(AppExitBehaviorSettingsKey) is int value)
            return (AppExitBehavior)value;

        return AppExitBehavior.RunInBackground;
    }

    public void SetAppExitBehavior(AppExitBehavior appExitBehavior)
        => _localApplicationData.SetValue(AppExitBehaviorSettingsKey, (int)appExitBehavior);
}
