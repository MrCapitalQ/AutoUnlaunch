using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.AppData;

namespace MrCapitalQ.AutoUnlaunch.Core.Launchers;

public abstract class LauncherHandler(LauncherSettingsService launcherSettingsService,
    TimeProvider timeProvider,
    ILogger logger) : ILauncherHandler
{
    private readonly LauncherSettingsService _launcherSettingsService = launcherSettingsService;
    protected readonly TimeProvider _timeProvider = timeProvider;
    protected readonly ILogger _logger = logger;

    private bool? _isLauncherCheckEnabled;
    private bool? _isLauncherRunning;
    private bool _shouldStopLauncher = false;
    private DateTimeOffset? _scheduledStopTime;

    public virtual async Task InvokeAsync(CancellationToken cancellationToken)
    {
        var isLauncherCheckEnabled = _launcherSettingsService.GetIsLauncherEnabled() ?? false;
        if (isLauncherCheckEnabled != _isLauncherCheckEnabled)
        {
            _isLauncherCheckEnabled = isLauncherCheckEnabled;
            _logger.LogDebug("Launcher handler for {LauncherName} is {LauncherHandlerState}.",
                LauncherName,
                _isLauncherCheckEnabled == true ? "enabled" : "disabled");
        }

        if (_isLauncherCheckEnabled != true)
            return;

        _logger.LogTrace("Launcher handler for {LauncherName} invoked.", LauncherName);

        var isLauncherRunning = await IsLauncherRunningAsync(cancellationToken);
        if (_isLauncherRunning != isLauncherRunning)
        {
            _isLauncherRunning = isLauncherRunning;
            _logger.LogDebug("{LauncherName} is currently {LauncherRunningState}.",
                LauncherName,
                _isLauncherRunning == true ? "running" : "not running");
        }

        if (_isLauncherRunning != true)
        {
            if (_scheduledStopTime is not null)
                _logger.LogInformation("{LauncherName} is not currently running. Cancelling scheduled stop.", LauncherName);

            _shouldStopLauncher = false;
            _scheduledStopTime = null;
            return;
        }

        if (await IsLauncherActivityRunningAsync(cancellationToken))
        {
            if (!_shouldStopLauncher)
            {
                _logger.LogInformation("An activity for {LauncherName} is currently running.", LauncherName);

                _shouldStopLauncher = true;
                await OnLauncherActivityStarted(cancellationToken);
            }
            else if (_scheduledStopTime is not null)
            {
                _logger.LogInformation("An activity for {LauncherName} is currently running. Cancelling scheduled stop.", LauncherName);

                _scheduledStopTime = null;
            }
            return;
        }

        if (!_shouldStopLauncher)
            return;

        if (_scheduledStopTime is null)
        {
            var stopDelayInSeconds = _launcherSettingsService.GetLauncherStopDelay() ?? 5;
            _logger.LogInformation("An activity for {LauncherName} is no longer running. Stopping launcher in {StopDelay} second(s).",
                LauncherName,
                stopDelayInSeconds);

            _scheduledStopTime = _timeProvider.GetUtcNow().AddSeconds(stopDelayInSeconds);
            await OnLauncherActivityEnded(cancellationToken);
        }

        if (_scheduledStopTime.Value <= _timeProvider.GetUtcNow())
        {
            _logger.LogInformation("Stopping {LauncherName}.", LauncherName);

            await StopLauncherAsync(cancellationToken);
            _shouldStopLauncher = false;
            _scheduledStopTime = null;
        }
    }

    protected abstract string LauncherName { get; }

    protected abstract Task<bool> IsLauncherRunningAsync(CancellationToken cancellationToken);
    protected abstract Task<bool> IsLauncherActivityRunningAsync(CancellationToken cancellationToken);
    protected abstract Task StopLauncherAsync(CancellationToken cancellationToken);
    protected virtual Task OnLauncherActivityStarted(CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task OnLauncherActivityEnded(CancellationToken cancellationToken) => Task.CompletedTask;
}
