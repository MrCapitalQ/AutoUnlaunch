using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using System.Diagnostics.CodeAnalysis;

namespace MrCapitalQ.AutoUnlaunch.Hosts;

[ExcludeFromCodeCoverage]
internal class LauncherBackgroundService : BackgroundService
{
    private const int LauncherCheckInterval = 1;

    private readonly ISet<ILauncherHandler> _handlers;
    private readonly ILogger<LauncherBackgroundService> _logger;

    public LauncherBackgroundService(IEnumerable<ILauncherHandler> handlers, ILogger<LauncherBackgroundService> logger)
    {
        _handlers = handlers.ToHashSet();
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Background launcher service checking for activity every {LauncherCheckInterval} second(s).", LauncherCheckInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogTrace($"Invoking launcher handlers.");

            var tasks = _handlers.Select(async x =>
            {
                _logger.LogTrace("Invoking launcher handler {LauncherHandlerType}.", x.GetType().FullName);
                try
                {
                    await x.InvokeAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Invocation of launcher handler {LauncherHandlerType} failed.", x.GetType().FullName);
                }
            });

            await Task.WhenAll(tasks);

            await Task.Delay(TimeSpan.FromSeconds(LauncherCheckInterval), stoppingToken);
        }

        _logger.LogInformation("Background launcher service stopping.");
    }
}
