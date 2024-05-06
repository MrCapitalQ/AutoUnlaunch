namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public class GogSettingsService : LauncherSettingsService
{
    public GogSettingsService(IApplicationDataStore applicationDataStore) : base(applicationDataStore)
    { }

    protected override string LauncherKey => "GOG";
}
