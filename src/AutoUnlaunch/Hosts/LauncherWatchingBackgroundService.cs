using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Diagnostics.CodeAnalysis;

namespace MrCapitalQ.AutoUnlaunch.Hosts;

[ExcludeFromCodeCoverage]
internal partial class LauncherWatchingBackgroundService(IEnumerable<ILauncherWatchingHandler> handlers,
    IMessenger messenger,
    ILogger<LauncherWatchingBackgroundService> logger) : BackgroundService
{
    private readonly ISet<ILauncherWatchingHandler> _handlers = handlers.ToHashSet();
    private readonly IMessenger _messenger = messenger;
    private readonly ILogger<LauncherWatchingBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await UpdateHandlerRunningStateAsync();

        _messenger.Register<LauncherHandlerIsEnabledChangedMessage>(this, async (r, m) =>
        {
            _logger.LogDebug("A launcher handler was enabled or disabled.");

            try
            {
                await UpdateHandlerRunningStateAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Something went wrong while starting and stopping handlers.");
            }
        });

        await Task.Delay(-1, stoppingToken);
    }

    private async Task UpdateHandlerRunningStateAsync()
    {
        foreach (var handler in _handlers)
        {
            if (handler.IsEnabled && !handler.IsStarted)
            {
                LogStartingHandler(handler.LauncherName);
                await handler.StartAsync();
            }
            else if (!handler.IsEnabled && handler.IsStarted)
            {
                LogStoppingHandler(handler.LauncherName);
                await handler.StopAsync();
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting handler for launcher {LauncherName} because it is enabled but is not currently currning.")]
    private partial void LogStartingHandler(string launcherName);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stopping handler for launcher {LauncherName} because it is disabled but is currently running.")]
    private partial void LogStoppingHandler(string launcherName);
}
