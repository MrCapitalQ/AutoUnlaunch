namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public class GogSettingsService(IApplicationDataStore applicationDataStore)
    : LauncherSettingsService(applicationDataStore)
{
    private const string HidesOnActivityEndSettingsKey = "HidesOnActivityEnd";

    protected override string LauncherKey => "GOG";
    protected override LauncherStopMethod DefaultLauncherStopMethod => LauncherStopMethod.RequestShutdown;

    public bool GetHidesOnActivityEnd() => GetValueOrDefault(HidesOnActivityEndSettingsKey, false);
    public void SetHidesOnActivityEnd(bool isEnabled) => SetValue(HidesOnActivityEndSettingsKey, isEnabled);
}
