using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core;
using System.Diagnostics;
using System.Management;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal class ProcessWatcher : IProcessWatcher
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

    public IEnumerable<ProcessInfo> GetCurrentProcesses()
    {
        foreach (var process in Process.GetProcesses())
        {
            ProcessInfo? processInfo = null;
            try
            {
                processInfo = new((uint)process.Id, process.ProcessName, process.MainModule?.FileName);
            }
            catch { }

            if (processInfo is not null)
                yield return processInfo;
        }
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
