using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppLifecycle;
using MrCapitalQ.AutoUnlaunch;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Hosts;
using MrCapitalQ.AutoUnlaunch.Infrastructure;
using MrCapitalQ.AutoUnlaunch.Settings;
using MrCapitalQ.AutoUnlaunch.Settings.Launchers.Steam;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
internal class Program
{
    private static async Task Main(string[] args)
    {
        var keyInstance = AppInstance.FindOrRegisterForKey("fa1bd88a-766a-45eb-be78-e9fc27e995bf");
        if (!keyInstance.IsCurrent)
        {
            var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
            await keyInstance.RedirectActivationToAsync(activationArgs);
            return;
        }

        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddHostedService<WindowsAppHostedService<App>>();
        builder.Services.AddHostedService<LauncherBackgroundService>();
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddSingleton<App>();
        builder.Services.AddSingleton<LifetimeWindow>();
        builder.Services.AddTransient<MainWindow>();

        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddSingleton<SettingsViewModel>();
        builder.Services.AddSingleton<ISteamSettingsViewModel, SteamSettingsViewModel>();

        builder.Services.AddStartupTaskService();
        builder.Services.AddLocalApplicationDataStore();
        builder.Services.AddSettingsService();
        builder.Services.AddProtocolLauncher();
        builder.Services.AddSteam();

        builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
        builder.Services.AddTransient<IPackageInfo, PackageInfo>();

        var host = builder.Build();
        host.Run();
    }
}
