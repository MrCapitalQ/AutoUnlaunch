using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Diagnostics.CodeAnalysis;

namespace MrCapitalQ.AutoUnlaunch.Hosts;

[ExcludeFromCodeCoverage]
internal partial class LauncherTrackingBackgroundService(IEnumerable<ILauncherTrackingHandler> handlers,
    IMessenger messenger,
    ILogger<LauncherTrackingBackgroundService> logger) : BackgroundService
{
    private readonly ISet<ILauncherTrackingHandler> _handlers = handlers.ToHashSet();
    private readonly IMessenger _messenger = messenger;
    private readonly ILogger<LauncherTrackingBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await UpdateHandlerRunningStateAsync(stoppingToken);

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

    private async Task UpdateHandlerRunningStateAsync(CancellationToken cancellationToken = default)
    {
        foreach (var handler in _handlers)
        {
            if (handler.IsEnabled && !handler.IsStarted)
                await handler.StartAsync(cancellationToken);
            else if (!handler.IsEnabled && handler.IsStarted)
                await handler.StopAsync(cancellationToken);
        }
    }
}
