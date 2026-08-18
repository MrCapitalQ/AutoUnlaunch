using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using System.Collections.Concurrent;

namespace MrCapitalQ.AutoUnlaunch.Core.Launchers;

public abstract partial class LauncherWatchingHandler(IProcessWatcher processWatcher,
    LauncherSettingsService launcherSettingsService,
    ILogger logger) : ILauncherWatchingHandler
{
    private readonly IProcessWatcher _processWatcher = processWatcher;
    private readonly LauncherSettingsService _launcherSettingsService = launcherSettingsService;
    private readonly ILogger _logger = logger;
    private readonly IDictionary<uint, ProcessInfo> _runningProcesses = new ConcurrentDictionary<uint, ProcessInfo>();

    private bool _isStarted;
    private bool _isLauncherActivityRunning;
    private CancellationTokenSource? _delayedStopCts;

    public abstract string LauncherName { get; }

    public bool IsEnabled => _launcherSettingsService.GetIsLauncherEnabled();

    protected virtual Task<bool> IsLauncherActivityRunningAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_isLauncherActivityRunning);

    public async Task StartAsync()
    {
        if (_isStarted)
        {
            LogHandlerAlreadyStarted(LauncherName);
            return;
        }

        await StopAsync();

        LogStartingHandler(LauncherName);

        _isStarted = true;

        _processWatcher.ProcessStarted += ProcessWatcher_ProcessStartedAsync;
        _processWatcher.ProcessStopped += ProcessWatcher_ProcessStopped;

        foreach (var processInfo in _processWatcher.GetCurrentProcesses())
        {
            if (await IsLauncherActivityAsync(processInfo))
                _runningProcesses[processInfo.ProcessId] = processInfo;
        }
    }

    public Task StopAsync()
    {
        if (!_isStarted)
        {
            LogHandlerAlreadyStopped(LauncherName);
            return Task.CompletedTask;
        }

        LogStoppingHandler(LauncherName);

        _isStarted = false;

        _processWatcher.ProcessStarted -= ProcessWatcher_ProcessStartedAsync;
        _processWatcher.ProcessStopped -= ProcessWatcher_ProcessStopped;

        _runningProcesses.Clear();
        _isLauncherActivityRunning = false;
        CancelPendingStop();

        return Task.CompletedTask;
    }

    protected abstract Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken = default);
    protected abstract Task<bool> IsLauncherActivityAsync(ProcessInfo processInfo);
    protected abstract Task StopLauncherAsync(CancellationToken cancellationToken = default);
    protected virtual Task OnLauncherActivityStarted(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnLauncherActivityEnded(CancellationToken cancellationToken = default) => Task.CompletedTask;

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

        if (!_isLauncherActivityRunning)
        {
            _isLauncherActivityRunning = true;

            CancelPendingStop();

            await OnLauncherActivityStarted();
        }
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

        if (!await IsLauncherRunningAsync())
        {
            LogLauncherNotRunning(LauncherName);
            return;
        }

        await OnLauncherActivityEnded();

        var stopDelayInSeconds = _launcherSettingsService.GetLauncherStopDelay();
        LogSchedulingLauncherStop(LauncherName, stopDelayInSeconds);

        _delayedStopCts = new CancellationTokenSource();
        await Task.Delay(TimeSpan.FromSeconds(stopDelayInSeconds), _delayedStopCts.Token).ContinueWith(async x =>
        {
            if (x.IsCanceled)
            {
                LogLauncherStopCancelled(LauncherName);
                return;
            }

            if (!await IsLauncherRunningAsync())
            {
                LogLauncherNoLongerRunning(LauncherName);
                return;
            }

            LogStoppingLauncher(LauncherName);
            await StopLauncherAsync();
        });
    }

    private void CancelPendingStop()
    {
        _delayedStopCts?.Cancel();
        _delayedStopCts = null;
    }

    [LoggerMessage(Level = LogLevel.Trace, Message = "Handler for {LauncherName} is already started.")]
    private partial void LogHandlerAlreadyStarted(string launcherName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting handler for {LauncherName}.")]
    private partial void LogStartingHandler(string launcherName);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Handler for launcher {LauncherName} is already stopped.")]
    private partial void LogHandlerAlreadyStopped(string launcherName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping handler for launcher {LauncherName}.")]
    private partial void LogStoppingHandler(string launcherName);

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

    [LoggerMessage(Level = LogLevel.Debug, Message = "Launcher {LauncherName} is not currently running. Launcher will not be stopped.")]
    private partial void LogLauncherNotRunning(string launcherName);

    [LoggerMessage(Level = LogLevel.Information, Message = "An activity for launcher {LauncherName} is no longer running. Stopping launcher in {StopDelay} second(s).")]
    private partial void LogSchedulingLauncherStop(string launcherName, int stopDelay);

    [LoggerMessage(Level = LogLevel.Information, Message = "Scheduled stop for launcher {LauncherName} was cancelled.")]
    private partial void LogLauncherStopCancelled(string launcherName);

    [LoggerMessage(Level = LogLevel.Information, Message = "{LauncherName} was scheduled to be stopped but it is no longer running.")]
    private partial void LogLauncherNoLongerRunning(string launcherName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping launcher {LauncherName}.")]
    private partial void LogStoppingLauncher(string launcherName);
}