using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using System.Collections.Concurrent;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;


internal abstract class LauncherWatchingHandler : ILauncherWatchingHandler
{
    private readonly ProcessWatcher _processWatcher;
    private readonly LauncherSettingsService _launcherSettingsService;
    private readonly ILogger _logger;
    private readonly IDictionary<uint, ProcessInfo> _runningProcesses = new ConcurrentDictionary<uint, ProcessInfo>();

    private bool _isLauncherActivityRunning;
    private CancellationTokenSource? _delayedStopCts;

    public LauncherWatchingHandler(ProcessWatcher processWatcher, LauncherSettingsService launcherSettingsService, ILogger logger)
    {
        _processWatcher = processWatcher;
        _launcherSettingsService = launcherSettingsService;
        _logger = logger;

        _processWatcher.ProcessStarted += ProcessWatcher_ProcessStartedAsync;
        _processWatcher.ProcessStopped += ProcessWatcher_ProcessStopped;
    }

    protected abstract string LauncherName { get; }

    protected virtual Task<bool> IsLauncherActivityRunningAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_isLauncherActivityRunning);

    protected abstract Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken = default);
    protected abstract bool IsLauncherActivity(ProcessInfo processInfo);
    protected abstract Task StopLauncherAsync(CancellationToken cancellationToken = default);
    protected virtual Task OnLauncherActivityStarted(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnLauncherActivityEnded(CancellationToken cancellationToken = default) => Task.CompletedTask;

    private async void ProcessWatcher_ProcessStartedAsync(object? sender, ProcessEventArgs e)
    {
        // TODO: Capture/handle errors in this async void method
        if (!IsLauncherActivity(e.ProcessInfo))
            return;

        _logger.LogInformation("An activity for {LauncherName} started with process info {ProcessInfo}.",
            LauncherName,
            e.ProcessInfo);
        _runningProcesses[e.ProcessInfo.ProcessId] = e.ProcessInfo;

        if (!_isLauncherActivityRunning)
        {
            _isLauncherActivityRunning = true;

            _delayedStopCts?.Cancel();
            _delayedStopCts = null;

            await OnLauncherActivityStarted();
        }

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

    public async Task StartWatchingAsync()
    {
        await Task.CompletedTask;
    }
}
