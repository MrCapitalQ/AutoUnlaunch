namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public class EASettingsService(IApplicationDataStore applicationDataStore)
    : LauncherSettingsService(applicationDataStore)
{
    private const string MinimizesOnActivityEndSettingsKey = "MinimizesOnActivityEnd";

    protected override string LauncherKey => "EA";
    protected override LauncherStopMethod DefaultLauncherStopMethod => LauncherStopMethod.CloseMainWindow;

    public bool GetMinimizesOnActivityEnd() => GetValueOrDefault(MinimizesOnActivityEndSettingsKey, false);
    public void SetMinimizesOnActivityEnd(bool isEnabled) => SetValue(MinimizesOnActivityEndSettingsKey, isEnabled);
}
