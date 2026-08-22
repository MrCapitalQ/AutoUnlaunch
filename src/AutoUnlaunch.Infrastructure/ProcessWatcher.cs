using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core;
using System.Diagnostics;
using System.Management;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

internal partial class ProcessWatcher : IProcessWatcher
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
            if (ProcessStarted is null)
                return;

            var targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;

            OnProcessStarted(ToProcessInfo(targetInstance));
        };

        var processStopWatcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_Process'"));
        processStopWatcher.EventArrived += (sender, e) =>
        {
            if (ProcessStopped is null)
                return;

            var targetInstance = (ManagementBaseObject)e.NewEvent.Properties["TargetInstance"].Value;

            OnProcessStopped(ToProcessInfo(targetInstance));
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
        if (raiseEvent is null)
            return;

        LogProcessStarted(processInfo);
        raiseEvent.Invoke(this, new ProcessEventArgs(processInfo));
    }

    protected void OnProcessStopped(ProcessInfo processInfo)
    {
        var raiseEvent = ProcessStopped;
        if (raiseEvent is null)
            return;

        LogProcessStopped(processInfo);
        raiseEvent.Invoke(this, new ProcessEventArgs(processInfo));
    }

    private ProcessInfo ToProcessInfo(ManagementBaseObject targetInstance)
    {
        var processId = (uint)targetInstance.Properties["ProcessId"].Value;
        var processName = targetInstance.Properties["Description"].Value?.ToString() ?? string.Empty;
        string? processPath = null;

        try
        {
            using var process = Process.GetProcessById((int)processId);
            processPath = process.MainModule?.FileName;
        }
        catch
        {
            LogFailedToGetProcessPath(processId);
        }

        return new(processId, processName, processPath);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Process started: {ProcessInfo}")]
    private partial void LogProcessStarted(ProcessInfo processInfo);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Process stopped: {ProcessInfo}")]
    private partial void LogProcessStopped(ProcessInfo processInfo);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Failed to get process path for process {ProcessId}.")]
    private partial void LogFailedToGetProcessPath(uint processId);
}
