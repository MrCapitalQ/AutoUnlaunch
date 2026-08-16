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
        UpdateHandlerRunningState();

        _messenger.Register<LauncherHandlerIsEnabledChangedMessage>(this, async (r, m) =>
        {
            _logger.LogInformation("A handler was enabled or disabled. Starting and stopping handlers as needed.");

            await Task.Run(() =>
            {
                try
                {
                    UpdateHandlerRunningState();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Something went wrong while starting and stopping handlers.");
                }
            });
        });

        await Task.Delay(-1, stoppingToken);
    }

    private void UpdateHandlerRunningState()
    {
        foreach (var handler in _handlers)
        {
            if (handler.IsEnabled)
                handler.Start();
            else
                handler.Stop();
        }
    }
}
