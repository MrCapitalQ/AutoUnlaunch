using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using System.Collections.Concurrent;

namespace MrCapitalQ.AutoUnlaunch.Core.Launchers;

public abstract class LauncherWatchingHandler(IProcessWatcher processWatcher,
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

    public void Start()
    {
        if (_isStarted)
        {
            _logger.LogTrace("Handler for {LauncherName} is already started.", LauncherName);
            return;
        }

        Stop();

        _logger.LogInformation("Starting handler for {LauncherName}.", LauncherName);

        _isStarted = true;

        _processWatcher.ProcessStarted += ProcessWatcher_ProcessStartedAsync;
        _processWatcher.ProcessStopped += ProcessWatcher_ProcessStopped;

        foreach (var processInfo in _processWatcher.GetCurrentProcesses())
        {
            if (IsLauncherActivity(processInfo))
                _runningProcesses[processInfo.ProcessId] = processInfo;
        }
    }

    public void Stop()
    {
        if (!_isStarted)
        {
            _logger.LogTrace("Handler for {LauncherName} is already stopped.", LauncherName);
            return;
        }

        _logger.LogInformation("Stopping handler for {LauncherName}.", LauncherName);

        _isStarted = false;

        _processWatcher.ProcessStarted -= ProcessWatcher_ProcessStartedAsync;
        _processWatcher.ProcessStopped -= ProcessWatcher_ProcessStopped;

        _runningProcesses.Clear();
        _isLauncherActivityRunning = false;
        CancelPendingStop();
    }

    protected abstract Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken = default);
    protected abstract bool IsLauncherActivity(ProcessInfo processInfo);
    protected abstract Task StopLauncherAsync(CancellationToken cancellationToken = default);
    protected virtual Task OnLauncherActivityStarted(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnLauncherActivityEnded(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private async void ProcessWatcher_ProcessStartedAsync(object? sender, ProcessEventArgs e)
    {
        // TODO: Capture/handle errors in this async void method
        await HandleProcessStartAsync(e.ProcessInfo);
    }

    private async void ProcessWatcher_ProcessStopped(object? sender, ProcessEventArgs e)
    {
        // TODO: Capture/handle errors in this async void method
        if (!_runningProcesses.Remove(e.ProcessInfo.ProcessId))
            return;

        _logger.LogInformation("An activity for {LauncherName} stopped with process info {ProcessInfo}.",
            LauncherName,
            e.ProcessInfo);

        _isLauncherActivityRunning = _runningProcesses.Count > 0;

        if (await IsLauncherActivityRunningAsync() || !await IsLauncherRunningAsync())
            return;

        await OnLauncherActivityEnded();

        var stopDelayInSeconds = _launcherSettingsService.GetLauncherStopDelay();
        _logger.LogInformation("An activity for {LauncherName} is no longer running. Stopping launcher in {StopDelay} second(s).",
            LauncherName,
            stopDelayInSeconds);

        _delayedStopCts = new CancellationTokenSource();
        await Task.Delay(TimeSpan.FromSeconds(stopDelayInSeconds), _delayedStopCts.Token).ContinueWith(async x =>
        {
            if (x.IsCanceled)
            {
                _logger.LogInformation("Scheduled stop for {LauncherName} was cancelled.", LauncherName);
                return;
            }

            if (!await IsLauncherRunningAsync())
            {
                _logger.LogInformation("{LauncherName} is no longer running.", LauncherName);
                return;
            }

            _logger.LogInformation("Stopping {LauncherName}.", LauncherName);
            await StopLauncherAsync();
        });
    }

    private async Task HandleProcessStartAsync(ProcessInfo processInfo)
    {
        _logger.LogDebug("Handling {LauncherName} process start detection for process {ProcessInfo}.",
            LauncherName,
            processInfo);

        if (_runningProcesses.ContainsKey(processInfo.ProcessId))
        {
            _logger.LogDebug("Activity for {LauncherName} already being tracked for process {ProcessInfo}.",
                LauncherName,
                processInfo);
            return;
        }

        if (!IsLauncherActivity(processInfo))
        {
            _logger.LogDebug("No activity for {LauncherName} matched for for process {ProcessInfo}.",
                LauncherName,
                processInfo);
            return;
        }

        _logger.LogInformation("An activity for {LauncherName} started with process info {ProcessInfo}.",
            LauncherName,
            processInfo);
        _runningProcesses[processInfo.ProcessId] = processInfo;

        if (!_isLauncherActivityRunning)
        {
            _isLauncherActivityRunning = true;

            CancelPendingStop();

            await OnLauncherActivityStarted();
        }
    }

    private void CancelPendingStop()
    {
        _delayedStopCts?.Cancel();
        _delayedStopCts = null;
    }
}
