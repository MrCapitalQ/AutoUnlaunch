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
        services.AddMemoryCache();
        services.TryAddSingleton<IApplicationDataStore, LocalApplicationDataStore>();
        return services;
    }

    public static IServiceCollection AddProtocolLauncher(this IServiceCollection services)
    {
        services.TryAddTransient<IProtocolLauncher, ProtocolLauncher>();
        return services;
    }

    public static IServiceCollection AddProcessWatcher(this IServiceCollection services)
    {
        services.TryAddSingleton<IProcessWatcher, ProcessWatcher>();
        return services;
    }

    public static IServiceCollection AddRegistryWatcher(this IServiceCollection services)
    {
        services.TryAddSingleton<RegistryWatcherFactory>();
        return services;
    }

    public static IServiceCollection AddProcessWindowService(this IServiceCollection services)
    {
        services.TryAddTransient<ProcessWindowService>();
        return services;
    }

    public static IServiceCollection AddSteam(this IServiceCollection services)
    {
        services.AddRegistryWatcher();
        services.AddProtocolLauncher();
        services.AddProcessWindowService();
        services.AddSingleton<ILauncherTrackingHandler, SteamLauncherTrackingHandler>();
        services.TryAddTransient<SteamSettingsService>();
        return services;
    }

    public static IServiceCollection AddEA(this IServiceCollection services)
    {
        services.AddProcessWatcher();
        services.AddRegistryWatcher();
        services.AddSingleton<ILauncherTrackingHandler, EALauncherProcessTrackingHandler>();
        services.TryAddTransient<EASettingsService>();
        return services;
    }

    public static IServiceCollection AddGog(this IServiceCollection services)
    {
        services.AddProcessWatcher();
        services.AddRegistryWatcher();
        services.AddProcessWindowService();
        services.AddSingleton<ILauncherTrackingHandler, GogLauncherProcessTrackingHandler>();
        services.TryAddTransient<GogSettingsService>();
        return services;
    }

    public static IServiceCollection AddEpic(this IServiceCollection services)
    {
        services.AddProcessWatcher();
        services.AddProtocolLauncher();
        services.AddSingleton<ILauncherTrackingHandler, EpicLauncherProcessTrackingHandler>();
        services.TryAddTransient<EpicSettingsService>();
        return services;
    }
}
