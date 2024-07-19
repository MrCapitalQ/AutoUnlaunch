namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public class SteamSettingsService(IApplicationDataStore applicationDataStore)
    : LauncherSettingsService(applicationDataStore)
{
    private const string HidesShutdownScreenSettingsKey = "HidesShutdownScreen";
    private const string HidesOnActivityStartSettingsKey = "HidesOnActivityStart";
    private const string HidesOnActivityEndSettingsKey = "HidesOnActivityEnd";
    private const string ShowUnnestedInStartMenuSettingsKey = "ShowUnnestedInStartMenu";

    protected override string LauncherKey => "Steam";

    public bool? GetHidesShutdownScreen() => GetValue<bool?>(HidesShutdownScreenSettingsKey);
    public void SetHidesShutdownScreen(bool isEnabled) => SetValue(HidesShutdownScreenSettingsKey, isEnabled);

    public bool? GetHidesOnActivityStart() => GetValue<bool?>(HidesOnActivityStartSettingsKey);
    public void SetHidesOnActivityStart(bool isEnabled) => SetValue(HidesOnActivityStartSettingsKey, isEnabled);

    public bool? GetHidesOnActivityEnd() => GetValue<bool?>(HidesOnActivityEndSettingsKey);
    public void SetHidesOnActivityEnd(bool isEnabled) => SetValue(HidesOnActivityEndSettingsKey, isEnabled);

    public bool? GetShowUnnestedInStartMenu() => GetValue<bool?>(ShowUnnestedInStartMenuSettingsKey);
    public void SetShowUnnestedInStartMenu(bool isEnabled) => SetValue(ShowUnnestedInStartMenuSettingsKey, isEnabled);
}
