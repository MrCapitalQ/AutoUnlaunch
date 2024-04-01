namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public class EpicSettingsService : LauncherSettingsService
{
    public EpicSettingsService(IApplicationDataStore applicationDataStore) : base(applicationDataStore)
    { }

    protected override string LauncherKey => "Epic";
}
