using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Startup;

namespace MrCapitalQ.AutoUnlaunch.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStartupTaskService(this IServiceCollection services)
    {
        services.TryAddTransient<IStartupTaskService, StartupTaskService>();
        return services;
    }

    public static IServiceCollection AddLocalApplicationDataStore(this IServiceCollection services)
    {
        services.TryAddSingleton<IApplicationDataStore, LocalApplicationDataStore>();
        return services;
    }
}
