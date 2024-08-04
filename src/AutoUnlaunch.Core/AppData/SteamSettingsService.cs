namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public class SteamSettingsService(IApplicationDataStore applicationDataStore)
    : LauncherSettingsService(applicationDataStore)
{
    private const string HidesShutdownScreenSettingsKey = "HidesShutdownScreen";
    private const string HidesOnActivityStartSettingsKey = "HidesOnActivityStart";
    private const string HidesOnActivityEndSettingsKey = "HidesOnActivityEnd";
    private const string ShowUnnestedInStartMenuSettingsKey = "ShowUnnestedInStartMenu";

    protected override string LauncherKey => "Steam";
    protected override LauncherStopMethod DefaultLauncherStopMethod => LauncherStopMethod.RequestShutdown;

    public bool GetHidesShutdownScreen() => GetValueOrDefault(HidesShutdownScreenSettingsKey, false);
    public void SetHidesShutdownScreen(bool isEnabled) => SetValue(HidesShutdownScreenSettingsKey, isEnabled);

    public bool GetHidesOnActivityStart() => GetValueOrDefault(HidesOnActivityStartSettingsKey, false);
    public void SetHidesOnActivityStart(bool isEnabled) => SetValue(HidesOnActivityStartSettingsKey, isEnabled);

    public bool GetHidesOnActivityEnd() => GetValueOrDefault(HidesOnActivityEndSettingsKey, false);
    public void SetHidesOnActivityEnd(bool isEnabled) => SetValue(HidesOnActivityEndSettingsKey, isEnabled);

    public bool GetShowUnnestedInStartMenu() => GetValueOrDefault(ShowUnnestedInStartMenuSettingsKey, false);
    public void SetShowUnnestedInStartMenu(bool isEnabled) => SetValue(ShowUnnestedInStartMenuSettingsKey, isEnabled);
}
