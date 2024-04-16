using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MrCapitalQ.AutoUnlaunch.Core.AppData;

namespace MrCapitalQ.AutoUnlaunch.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSettingsService(this IServiceCollection services)
    {
        services.TryAddTransient<ISettingsService, SettingsService>();
        return services;
    }
}
