namespace MrCapitalQ.AutoUnlaunch.Core;

public interface IProcessWatcher
{
    event EventHandler<ProcessEventArgs>? ProcessStarted;
    event EventHandler<ProcessEventArgs>? ProcessStopped;

    IEnumerable<ProcessInfo> GetCurrentProcesses();
}

public class ProcessEventArgs(ProcessInfo processInfo) : EventArgs
{
    public ProcessInfo ProcessInfo { get; } = processInfo;
}

public record ProcessInfo(uint ProcessId, string ProcessName, string? ProcessPath);
