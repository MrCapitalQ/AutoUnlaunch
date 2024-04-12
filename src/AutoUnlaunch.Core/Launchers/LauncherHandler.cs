using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.AppData;

namespace MrCapitalQ.AutoUnlaunch.Core.Launchers;

public abstract class LauncherHandler : ILauncherHandler
{
    private readonly LauncherSettingsService _launcherSettingsService;
    protected readonly TimeProvider _timeProvider;
    protected readonly ILogger _logger;

    private bool _shouldStopLauncher = false;
    private DateTimeOffset? _scheduledStopTime;

    protected LauncherHandler(LauncherSettingsService launcherSettingsService,
        TimeProvider timeProvider,
        ILogger logger)
    {
        _launcherSettingsService = launcherSettingsService;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task InvokeAsync(CancellationToken cancellationToken)
    {
        if (_launcherSettingsService.GetIsLauncherEnabled() != true)
        {
            _logger.LogTrace("Launcher handler for {LauncherName} is disabled.", LauncherName);
            return;
        }
        else
            _logger.LogTrace("Launcher handler for {LauncherName} invoked.", LauncherName);

        if (!await IsLauncherRunningAsync(cancellationToken))
        {
            if (_scheduledStopTime is not null)
                _logger.LogInformation("{LauncherName} is not currently running. Cancelling scheduled stop.", LauncherName);
            else
                _logger.LogTrace("{LauncherName} is not currently running.", LauncherName);

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
