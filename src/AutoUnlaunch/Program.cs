using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppLifecycle;
using MrCapitalQ.AutoUnlaunch;
using MrCapitalQ.AutoUnlaunch.Core;
using MrCapitalQ.AutoUnlaunch.Infrastructure;
using MrCapitalQ.AutoUnlaunch.Settings;

var keyInstance = AppInstance.FindOrRegisterForKey("fa1bd88a-766a-45eb-be78-e9fc27e995bf");
if (!keyInstance.IsCurrent)
{
    var activationArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
    await keyInstance.RedirectActivationToAsync(activationArgs);
    return;
}

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<WindowsAppHostedService<App>>();

builder.Services.AddSingleton<App>();
builder.Services.AddSingleton<LifetimeWindow>();
builder.Services.AddTransient<MainWindow>();

builder.Services.AddSingleton<SettingsPage>();
builder.Services.AddSingleton<SettingsViewModel>();

builder.Services.AddStartupTaskService();
builder.Services.AddLocalApplicationDataStore();
builder.Services.AddSettingsService();

builder.Services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);

var host = builder.Build();
host.Run();