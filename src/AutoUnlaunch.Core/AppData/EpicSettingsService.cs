namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public class EpicSettingsService(IApplicationDataStore applicationDataStore)
    : LauncherSettingsService(applicationDataStore)
{
    protected override string LauncherKey => "Epic";
    protected override LauncherStopMethod DefaultLauncherStopMethod => LauncherStopMethod.CloseMainWindow;
}
