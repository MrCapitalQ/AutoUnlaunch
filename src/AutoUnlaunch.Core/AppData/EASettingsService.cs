namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public class EASettingsService : LauncherSettingsService
{
    private const string MinimizesOnActivityEndSettingsKey = "MinimizesOnActivityEnd";

    public EASettingsService(IApplicationDataStore applicationDataStore) : base(applicationDataStore)
    { }

    protected override string LauncherKey => "EA";

    public bool? GetMinimizesOnActivityEnd() => GetValue<bool?>(MinimizesOnActivityEndSettingsKey);
    public void SetMinimizesOnActivityEnd(bool isEnabled) => SetValue(MinimizesOnActivityEndSettingsKey, isEnabled);
}
