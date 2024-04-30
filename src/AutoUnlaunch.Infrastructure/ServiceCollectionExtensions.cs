using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Core.Launchers;
using MrCapitalQ.AutoUnlaunch.Core.Startup;
using MrCapitalQ.AutoUnlaunch.Infrastructure.Launchers;

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

    public static IServiceCollection AddProtocolLauncher(this IServiceCollection services)
    {
        services.TryAddTransient<IProtocolLauncher, ProtocolLauncher>();
        return services;
    }

    public static IServiceCollection AddSteam(this IServiceCollection services)
    {
        services.AddSingleton<ILauncherHandler, SteamLauncherHandler>();
        services.TryAddTransient<SteamSettingsService>();
        services.AddProtocolLauncher();
        return services;
    }

    public static IServiceCollection AddEA(this IServiceCollection services)
    {
        services.AddSingleton<ILauncherHandler, EALauncherHandler>();
        services.TryAddTransient<EASettingsService>();
        services.TryAddTransient<LauncherChildProcessChecker>();
        return services;
    }
}
