namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public abstract class LauncherSettingsService
{
    private const string LauncherSettingsKeyTemplate = "{0}_{1}";
    private const string IsEnabledSettingsKey = "IsEnabled";
    private const string StopDelaySettingsKey = "StopDelay";
    private const string StopMethodSettingsKey = "StopMethod";

    private readonly IApplicationDataStore _applicationDataStore;

    protected LauncherSettingsService(IApplicationDataStore applicationDataStore)
        => _applicationDataStore = applicationDataStore;

    protected abstract string LauncherKey { get; }

    public bool? GetIsLauncherEnabled() => GetValue<bool?>(IsEnabledSettingsKey);
    public void SetIsLauncherEnabled(bool isEnabled) => SetValue(IsEnabledSettingsKey, isEnabled);

    public int? GetLauncherStopDelay() => GetValue<int?>(StopDelaySettingsKey);
    public void SetLauncherStopDelay(int delay) => SetValue(StopDelaySettingsKey, delay);

    public LauncherStopMethod? GetLauncherStopMethod() => (LauncherStopMethod?)GetValue<int?>(StopMethodSettingsKey);
    public void SetLauncherStopMethod(LauncherStopMethod stopMethod) => SetValue(StopMethodSettingsKey, (int)stopMethod);

    protected T? GetValue<T>(string key)
    {
        if (_applicationDataStore.GetValue(GetKeyForLauncher(key)) is not null and T value)
            return value;

        return default;
    }
    protected void SetValue<T>(string key, T? value) => _applicationDataStore.SetValue(GetKeyForLauncher(key), value);

    private string GetKeyForLauncher(string key) => string.Format(LauncherSettingsKeyTemplate, LauncherKey, key);
}
