using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Xaml;
using System.Runtime.InteropServices;

namespace MrCapitalQ.AutoUnlaunch;

public partial class App : Application
{
    public App(IServiceProvider services)
    {
        InitializeComponent();
        Services = services;
    }

    public static new App Current => (App)Application.Current;
    public IServiceProvider Services { get; }
    public Window? Window { get; protected set; }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _ = SetPreferredAppMode(PreferredAppMode.AllowDark);

        Window = Services.GetRequiredService<MainWindow>();
        Window.Activate();
        Window.Closed += Window_Closed;
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        if (sender is Window window)
            window.Closed -= Window_Closed;

        Window = null;

        var hostApplicationLifetime = Services.GetRequiredService<IHostApplicationLifetime>();
        hostApplicationLifetime.StopApplication();
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
