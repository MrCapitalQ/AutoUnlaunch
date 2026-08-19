using Microsoft.Extensions.Logging;

namespace MrCapitalQ.AutoUnlaunch.Core;

public static partial class LoggerExtensions
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Request to gracefully shutdown {LauncherName} succeeded.")]
    public static partial void LogGracefulShutdownSucceeded(this ILogger logger, string launcherName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Killing process {ProcessName} ({ProcessId}).")]
    public static partial void LogKillingProcess(this ILogger logger, string processName, int processId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Closing current main window with title '{WindowTitle}' ({WindowHandle}) for process {ProcessName} ({ProcessId}).")]
    public static partial void LogClosingProcessMainWindow(this ILogger logger,
        string windowTitle,
        nint windowHandle,
        string processName,
        int processId);
}
