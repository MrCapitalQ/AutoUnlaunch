using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Diagnostics.CodeAnalysis;

namespace MrCapitalQ.AutoUnlaunch.Settings.Launchers.Steam;

[ExcludeFromCodeCoverage(Justification = ExcludeFromCoverageJustifications.RequiresUIThread)]
public sealed partial class SteamSettingsPage : Page
{
    private readonly ISteamSettingsViewModel _viewModel;

    public SteamSettingsPage()
    {
        InitializeComponent();
        _viewModel = App.Current.Services.GetRequiredService<ISteamSettingsViewModel>();
    }
}
