using Microsoft.Extensions.Logging;

namespace MrCapitalQ.AutoUnlaunch.Core.AppData;

public interface ISettingsService
{
    bool GetHasBeenLaunchedOnce();
    void SetHasBeenLaunchedOnce();

    AppExitBehavior GetAppExitBehavior();
    void SetAppExitBehavior(AppExitBehavior appExitBehavior);

    LogLevel GetMinimumLogLevel();
    void SetMinimumLogLevel(LogLevel logLevel);
}