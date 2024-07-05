namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public class GogSettingsService(IApplicationDataStore applicationDataStore)
    : LauncherSettingsService(applicationDataStore)
{
    private const string HidesOnActivityEndSettingsKey = "HidesOnActivityEnd";

    protected override string LauncherKey => "GOG";

    public bool? GetHidesOnActivityEnd() => GetValue<bool?>(HidesOnActivityEndSettingsKey);
    public void SetHidesOnActivityEnd(bool isEnabled) => SetValue(HidesOnActivityEndSettingsKey, isEnabled);
}
