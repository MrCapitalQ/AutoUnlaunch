namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public abstract class LauncherSettingsService(IApplicationDataStore applicationDataStore)
{
    private const string LauncherSettingsKeyTemplate = "{0}_{1}";
    private const string IsEnabledSettingsKey = "IsEnabled";
    private const string StopDelaySettingsKey = "StopDelay";
    private const string StopMethodSettingsKey = "StopMethod";

    private const int DefaultLauncherStopDelay = 5;

    private readonly IApplicationDataStore _applicationDataStore = applicationDataStore;

    protected abstract string LauncherKey { get; }
    protected abstract LauncherStopMethod DefaultLauncherStopMethod { get; }

    public bool GetIsLauncherEnabled() => GetValueOrDefault(IsEnabledSettingsKey, true);
    public void SetIsLauncherEnabled(bool isEnabled) => SetValue(IsEnabledSettingsKey, isEnabled);

    public int GetLauncherStopDelay() => GetValueOrDefault(StopDelaySettingsKey, DefaultLauncherStopDelay);
    public void SetLauncherStopDelay(int delay) => SetValue(StopDelaySettingsKey, delay);

    public LauncherStopMethod GetLauncherStopMethod() => (LauncherStopMethod)GetValueOrDefault(StopMethodSettingsKey, (int)DefaultLauncherStopMethod);
    public void SetLauncherStopMethod(LauncherStopMethod stopMethod) => SetValue(StopMethodSettingsKey, (int)stopMethod);

    protected T GetValueOrDefault<T>(string key, T defaultValue) => _applicationDataStore.GetValueOrDefault(GetKeyForLauncher(key), defaultValue);
    protected void SetValue<T>(string key, T? value) => _applicationDataStore.SetValue(GetKeyForLauncher(key), value);

    private string GetKeyForLauncher(string key) => string.Format(LauncherSettingsKeyTemplate, LauncherKey, key);
}
