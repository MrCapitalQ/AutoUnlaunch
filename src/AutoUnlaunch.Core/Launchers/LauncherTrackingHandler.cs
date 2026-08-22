using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.AppData;

namespace MrCapitalQ.AutoUnlaunch.Core.Launchers;

public abstract partial class LauncherTrackingHandler(LauncherSettingsService launcherSettingsService,
    TimeProvider timeProvider,
    ILogger logger)
    : ILauncherTrackingHandler
{
    private readonly LauncherSettingsService _launcherSettingsService = launcherSettingsService;
    private readonly TimeProvider _timeProvider = timeProvider;
    private readonly ILogger _logger = logger;

    private CancellationTokenSource? _delayedStopCts;

    public abstract string LauncherName { get; }
    public bool IsEnabled => _launcherSettingsService.GetIsLauncherEnabled();
    public bool IsStarted { get; protected set; }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (IsStarted)
        {
            LogHandlerAlreadyStarted(LauncherName);
            return;
        }

        await StopAsync(cancellationToken);

        LogStartingHandler(LauncherName);

        await OnStartingAsync(cancellationToken);

        IsStarted = true;

        await OnStartedAsync(cancellationToken);
    }
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (!IsStarted)
        {
            LogHandlerAlreadyStopped(LauncherName);
            return;
        }

        LogStoppingHandler(LauncherName);

        await OnStoppingAsync(cancellationToken);

        IsStarted = false;

        await OnStoppedAsync(cancellationToken);
    }

    protected virtual Task OnStartingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStartedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStoppingAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnStoppedAsync(CancellationToken cancellationToken = default)
    {
        CancelPendingStop();
        return Task.CompletedTask;
    }

    protected abstract Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken = default);
    protected abstract Task StopLauncherAsync(CancellationToken cancellationToken = default);
    protected virtual Task OnLauncherActivityStarted(CancellationToken cancellationToken = default) => Task.CompletedTask;
    protected virtual Task OnLauncherActivityEnded(CancellationToken cancellationToken = default) => Task.CompletedTask;

    protected async Task SetLauncherActivityStartedAsync(CancellationToken cancellationToken = default)
    {
        CancelPendingStop();
        await OnLauncherActivityStarted(cancellationToken);
    }

    protected async Task SetLauncherActivityEndedAsync(CancellationToken cancellationToken = default)
    {
        if (!await IsLauncherRunningAsync(cancellationToken))
        {
            LogLauncherNotRunning(LauncherName);
            return;
        }

        await OnLauncherActivityEnded(cancellationToken);

        var stopDelayInSeconds = _launcherSettingsService.GetLauncherStopDelay();
        LogSchedulingLauncherStop(LauncherName, stopDelayInSeconds);

        _delayedStopCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        await Task.Delay(TimeSpan.FromSeconds(stopDelayInSeconds), _timeProvider, _delayedStopCts.Token).ContinueWith(async x =>
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
    protected partial void LogHandlerAlreadyStarted(string launcherName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Starting handler for launcher {LauncherName}.")]
    protected partial void LogStartingHandler(string launcherName);

    [LoggerMessage(Level = LogLevel.Trace, Message = "Handler for launcher {LauncherName} is already stopped.")]
    protected partial void LogHandlerAlreadyStopped(string launcherName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping handler for launcher {LauncherName}.")]
    protected partial void LogStoppingHandler(string launcherName);

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