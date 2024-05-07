namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public class SteamSettingsService : LauncherSettingsService
{
    private const string HidesShutdownScreenSettingsKey = "HidesShutdownScreen";
    private const string HidesOnActivityStartSettingsKey = "HidesOnActivityStart";
    private const string HidesOnActivityEndSettingsKey = "HidesOnActivityEnd";

    public SteamSettingsService(IApplicationDataStore applicationDataStore) : base(applicationDataStore)
    { }

    protected override string LauncherKey => "Steam";

    public bool? GetHidesShutdownScreen() => GetValue<bool?>(HidesShutdownScreenSettingsKey);
    public void SetHidesShutdownScreen(bool isEnabled) => SetValue(HidesShutdownScreenSettingsKey, isEnabled);

    public bool? GetHidesOnActivityStart() => GetValue<bool?>(HidesOnActivityStartSettingsKey);
    public void SetHidesOnActivityStart(bool isEnabled) => SetValue(HidesOnActivityStartSettingsKey, isEnabled);

    public bool? GetHidesOnActivityEnd() => GetValue<bool?>(HidesOnActivityEndSettingsKey);
    public void SetHidesOnActivityEnd(bool isEnabled) => SetValue(HidesOnActivityEndSettingsKey, isEnabled);
}
