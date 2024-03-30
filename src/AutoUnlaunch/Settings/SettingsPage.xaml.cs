using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;

namespace MrCapitalQ.AutoUnlaunch.Settings;

public sealed partial class SettingsPage : Page
{
    private readonly SettingsViewModel _viewModel;

    public SettingsPage()
    {
        InitializeComponent();
        _viewModel = App.Current.Services.GetRequiredService<SettingsViewModel>();
    }
}
