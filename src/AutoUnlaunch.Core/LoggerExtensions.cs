using Microsoft.Extensions.Logging;

namespace MrCapitalQ.AutoUnlaunch.Core;

public static partial class LoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Killing process {ProcessName} ({ProcessId}).")]
    public static partial void LogKillingProcess(this ILogger logger, string processName, int processId);
}
