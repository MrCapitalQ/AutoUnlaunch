using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Diagnostics.CodeAnalysis;

namespace MrCapitalQ.AutoUnlaunch.Hosts;

[ExcludeFromCodeCoverage(Justification = ExcludeFromCoverageJustifications.RequiresUIThread)]
internal class WindowsAppHostedService<TApplication>(IHostApplicationLifetime hostApplicationLifetime,
    IServiceProvider serviceProvider,
    ILogger<WindowsAppHostedService<TApplication>> logger) : IHostedService where TApplication : Application
{
    private readonly IHostApplicationLifetime _hostApplicationLifetime = hostApplicationLifetime;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly ILogger<WindowsAppHostedService<TApplication>> _logger = logger;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var thread = new Thread(Main);
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void Main()
    {
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            var app = _serviceProvider.GetRequiredService<TApplication>();
            app.UnhandledException += App_UnhandledException;
        });
        _hostApplicationLifetime.StopApplication();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        => _logger.LogCritical(e.Exception, "An unhandled application exception has occurred.");
}
