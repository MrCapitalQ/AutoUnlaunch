using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Windows.AppLifecycle;
using MrCapitalQ.AutoUnlaunch;
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
builder.Services.AddSingleton<MainWindow>();

builder.Services.AddSingleton<SettingsPage>();
builder.Services.AddSingleton<SettingsViewModel>();

builder.Services.AddSingleton<StartupTaskService>();

var host = builder.Build();
host.Run();