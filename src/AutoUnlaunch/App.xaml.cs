using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using MrCapitalQ.AutoUnlaunch.Core.AppData;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Runtime.InteropServices;
using WinUIEx;

namespace MrCapitalQ.AutoUnlaunch;

public partial class App : Application
{
    public App(IServiceProvider services)
    {
        InitializeComponent();
        Services = services;

        AppInstance.GetCurrent().Activated += App_Activated;
    }

    public static new App Current => (App)Application.Current;
    public IServiceProvider Services { get; }
    public LifetimeWindow? LifetimeWindow { get; protected set; }
    public MainWindow? MainWindow { get; protected set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = SetPreferredAppMode(PreferredAppMode.AllowDark);

        Services.GetRequiredService<ISettingsService>().SetHasBeenLaunchedOnce();

        LifetimeWindow = Services.GetRequiredService<LifetimeWindow>();
        LifetimeWindow.Closed += LifetimeWindow_Closed;

        var messenger = Services.GetRequiredService<IMessenger>();
        messenger.Register<App, StopAppMessage>(this, (r, m) => r.LifetimeWindow?.Close());

        if (AppInstance.GetCurrent().GetActivatedEventArgs().Kind != ExtendedActivationKind.StartupTask
            && !Environment.GetCommandLineArgs().Contains("-silent"))
            ShowMainWindow();
    }

    private void ShowMainWindow()
    {
        if (MainWindow is null)
        {
            MainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow.Closed += MainWindow_Closed;
        }

        MainWindow.Activate();
        MainWindow.SetForegroundWindow();
    }

    private void LifetimeWindow_Closed(object sender, WindowEventArgs args)
    {
        if (sender is LifetimeWindow window)
            window.Closed -= LifetimeWindow_Closed;

        MainWindow?.Close();
        LifetimeWindow = null;

        var hostApplicationLifetime = Services.GetRequiredService<IHostApplicationLifetime>();
        hostApplicationLifetime.StopApplication();
    }

    private void App_Activated(object? sender, AppActivationArguments e)
    {
        if (e.Kind != ExtendedActivationKind.Launch)
            return;

        LifetimeWindow?.DispatcherQueue.TryEnqueue(ShowMainWindow);
    }

    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {
        if (sender is MainWindow window)
            window.Closed -= MainWindow_Closed;

        MainWindow = null;
    }

    [LibraryImport("uxtheme.dll", EntryPoint = "#135", SetLastError = true)]
    private static partial int SetPreferredAppMode(PreferredAppMode preferredAppMode);

    private enum PreferredAppMode
    {
        Default,
        AllowDark,
        ForceDark,
        ForceLight,
        Max
    };
}
