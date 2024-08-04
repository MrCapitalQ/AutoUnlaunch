using Microsoft.Extensions.Logging;

namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

internal class SettingsService(IApplicationDataStore localApplicationData) : ISettingsService
{
    private const string HasBeenLaunchedOnceSettingsKey = "HasBeenLaunchedOnce";
    private const string AppExitBehaviorSettingsKey = "AppExitBehavior";
    private const string MinimumLogLevelSettingsKey = "MinimumLogLevel";

    private readonly IApplicationDataStore _applicationDataStore = localApplicationData;

    public bool GetHasBeenLaunchedOnce()
        => _applicationDataStore.GetValue(HasBeenLaunchedOnceSettingsKey) is bool value && value;

    public void SetHasBeenLaunchedOnce()
        => _applicationDataStore.SetValue(HasBeenLaunchedOnceSettingsKey, true);

    public AppExitBehavior GetAppExitBehavior()
        => (AppExitBehavior)_applicationDataStore.GetValueOrDefault(AppExitBehaviorSettingsKey, (int)AppExitBehavior.RunInBackground);

    public void SetAppExitBehavior(AppExitBehavior appExitBehavior)
        => _applicationDataStore.SetValue(AppExitBehaviorSettingsKey, (int)appExitBehavior);

    public LogLevel GetMinimumLogLevel()
        => (LogLevel)_applicationDataStore.GetValueOrDefault(MinimumLogLevelSettingsKey, (int)LogLevel.Information);

    public void SetMinimumLogLevel(LogLevel logLevel)
        => _applicationDataStore.SetValue(MinimumLogLevelSettingsKey, (int)logLevel);
}
