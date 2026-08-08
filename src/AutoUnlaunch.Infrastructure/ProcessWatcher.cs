using Microsoft.Extensions.Logging;
using System.Management;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal class ProcessWatcher
{
    public event EventHandler<ProcessEventArgs>? ProcessStarted;
    public event EventHandler<ProcessEventArgs>? ProcessStopped;

    private readonly ILogger<ProcessWatcher> _logger;

    public ProcessWatcher(ILogger<ProcessWatcher> logger)
    {
        _logger = logger;

        var processStartWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'"));
        processStartWatcher.EventArrived += (sender, e) =>
        {
            var targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;

            OnProcessStarted(new((uint)targetInstance.Properties["ProcessId"].Value,
                targetInstance.Properties["Description"].Value?.ToString() ?? string.Empty,
                targetInstance.Properties["ExecutablePath"].Value?.ToString()));
        };

        var processStopWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'"));
        processStopWatcher.EventArrived += (sender, e) =>
        {
            var targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;

            OnProcessStopped(new((uint)targetInstance.Properties["ProcessId"].Value,
                targetInstance.Properties["Description"].Value?.ToString() ?? string.Empty,
                targetInstance.Properties["ExecutablePath"].Value?.ToString()));
        };

        processStartWatcher.Start();
        processStopWatcher.Start();
    }

    protected void OnProcessStarted(ProcessInfo processInfo)
    {
        var raiseEvent = ProcessStarted;
        raiseEvent?.Invoke(this, new ProcessEventArgs(processInfo));

        _logger.LogDebug("Process started: {ProcessInfo}", processInfo);
    }

    protected void OnProcessStopped(ProcessInfo processInfo)
    {
        var raiseEvent = ProcessStopped;
        raiseEvent?.Invoke(this, new ProcessEventArgs(processInfo));

        _logger.LogDebug("Process stopped: {ProcessInfo}", processInfo);
    }
}

internal class ProcessEventArgs(ProcessInfo processInfo) : EventArgs
{
    public ProcessInfo ProcessInfo { get; } = processInfo;
}

internal record ProcessInfo(uint ProcessId, string ProcessName, string? ProcessPath);
