using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using System.Diagnostics.CodeAnalysis;

namespace MrCapitalQ.AutoUnlaunch.Hosts;

[ExcludeFromCodeCoverage]
internal class LauncherWatchingBackgroundService(IEnumerable<ILauncherWatchingHandler> handlers,
    ILogger<LauncherWatchingBackgroundService> logger) : BackgroundService
{
    private readonly ISet<ILauncherWatchingHandler> _handlers = handlers.ToHashSet();
    private readonly ILogger<LauncherWatchingBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var handler in _handlers)
        {
            await handler.StartWatchingAsync();
        }
        await Task.Delay(-1, stoppingToken);
    }
}
