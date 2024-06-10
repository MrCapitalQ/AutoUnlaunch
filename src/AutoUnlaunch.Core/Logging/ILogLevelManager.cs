using Microsoft.Extensions.Logging;

namespace MrCapitalQ.AutoUnlaunch.Core.Logging;

public interface ILogLevelManager
{
    void SetMinimumLogLevel(LogLevel logLevel);
}
