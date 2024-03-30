using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MrCapitalQ.AutoUnlaunch;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddHostedService<WindowsAppHostedService<App>>();

builder.Services.AddSingleton<App>();
builder.Services.AddSingleton<MainWindow>();

var host = builder.Build();
host.Run();