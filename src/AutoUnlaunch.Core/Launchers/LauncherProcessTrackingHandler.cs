using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using System.Collections.Concurrent;

namespace MrCapitalQ.AutoUnlaunch.Core.Launchers;

public abstract partial class LauncherProcessTrackingHandler(IProcessWatcher processWatcher,
    LauncherSettingsService launcherSettingsService,
    TimeProvider timeProvider,
    ILogger logger) : LauncherBaseHandler(launcherSettingsService, timeProvider, logger)
{
    private readonly IProcessWatcher _processWatcher = processWatcher;
    private readonly ILogger _logger = logger;
    private readonly IDictionary<uint, ProcessInfo> _runningProcesses = new ConcurrentDictionary<uint, ProcessInfo>();

    private bool _isLauncherActivityRunning;

    protected override Task OnStartingAsync(CancellationToken cancellationToken = default)
    {
        _processWatcher.ProcessStarted += ProcessWatcher_ProcessStartedAsync;
        _processWatcher.ProcessStopped += ProcessWatcher_ProcessStopped;
        return base.OnStartingAsync(cancellationToken);
    }

    protected override async Task OnStartedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var processInfo in _processWatcher.GetCurrentProcesses())
        {
            if (await IsLauncherActivityAsync(processInfo))
                _runningProcesses[processInfo.ProcessId] = processInfo;
        }

        await base.OnStartedAsync(cancellationToken);
    }

    protected override Task OnStoppingAsync(CancellationToken cancellationToken = default)
    {
        _processWatcher.ProcessStarted -= ProcessWatcher_ProcessStartedAsync;
        _processWatcher.ProcessStopped -= ProcessWatcher_ProcessStopped;
        return base.OnStoppingAsync(cancellationToken);
    }

    protected override async Task OnStoppedAsync(CancellationToken cancellationToken = default)
    {
        await base.OnStoppedAsync(cancellationToken);
        _runningProcesses.Clear();
        _isLauncherActivityRunning = false;
    }

    protected virtual Task<bool> IsLauncherActivityRunningAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_isLauncherActivityRunning);
    protected abstract Task<bool> IsLauncherActivityAsync(ProcessInfo processInfo);

    private async void ProcessWatcher_ProcessStartedAsync(object? sender, ProcessEventArgs e)
    {
        try
        {
            await HandleProcessStartAsync(e.ProcessInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Something went wrong while handling process start event for launcher {LauncherName}.",
                LauncherName);
            throw;
        }
    }

    private async void ProcessWatcher_ProcessStopped(object? sender, ProcessEventArgs e)
    {
        try
        {
            await HandleProcessStopAsync(e.ProcessInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Something went wrong while handling process stop event for launcher {LauncherName}.",
                LauncherName);
            throw;
        }
    }

    private async Task HandleProcessStartAsync(ProcessInfo processInfo)
    {
        LogProcessStarted(LauncherName, processInfo);

        if (_runningProcesses.ContainsKey(processInfo.ProcessId))
        {
            LogProcessAlreadyTracked(LauncherName, processInfo);
            return;
        }

        if (!await IsLauncherActivityAsync(processInfo))
        {
            LogProcessNotActivity(LauncherName, processInfo);
            return;
        }

        LogActivityStarted(LauncherName, processInfo);

        _runningProcesses[processInfo.ProcessId] = processInfo;

        if (_isLauncherActivityRunning)
            return;

        _isLauncherActivityRunning = true;
        await SetLauncherActivityStartedAsync();
    }

    private async Task HandleProcessStopAsync(ProcessInfo processInfo)
    {
        LogProcessStopped(LauncherName, processInfo);

        if (!_runningProcesses.Remove(processInfo.ProcessId))
            return;

        LogActivityStopped(LauncherName, processInfo);

        _isLauncherActivityRunning = _runningProcesses.Count > 0;

        if (_isLauncherActivityRunning)
        {
            LogActivitiesStillRunning(_runningProcesses.Count, LauncherName);
            return;
        }

        await SetLauncherActivityEndedAsync();
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handling launcher {LauncherName} process start detection for process {ProcessInfo}.")]
    private partial void LogProcessStarted(string launcherName, ProcessInfo processInfo);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Activity for launcher {LauncherName} already being tracked for process {ProcessInfo}.")]
    private partial void LogProcessAlreadyTracked(string launcherName, ProcessInfo processInfo);

    [LoggerMessage(Level = LogLevel.Debug, Message = "No activity for launcher {LauncherName} matched for for process {ProcessInfo}.")]
    private partial void LogProcessNotActivity(string launcherName, ProcessInfo processInfo);

    [LoggerMessage(Level = LogLevel.Information, Message = "An activity for launcher {LauncherName} started with process info {ProcessInfo}.")]
    private partial void LogActivityStarted(string launcherName, ProcessInfo processInfo);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Handling launcher {LauncherName} process stop detection for process {ProcessInfo}.")]
    private partial void LogProcessStopped(string launcherName, ProcessInfo processInfo);

    [LoggerMessage(Level = LogLevel.Debug, Message = "An activity for launcher {LauncherName} stopped with process info {ProcessInfo}.")]
    private partial void LogActivityStopped(string launcherName, ProcessInfo processInfo);

    [LoggerMessage(Level = LogLevel.Debug, Message = "{LauncherActivityCount} activities for launcher {LauncherName} are still running. Launcher will not be stopped.")]
    private partial void LogActivitiesStillRunning(int launcherActivityCount, string launcherName);
}
