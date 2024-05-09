using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Diagnostics.CodeAnalysis;

namespace MrCapitalQ.AutoUnlaunch.Settings.Launchers.Gog;

[ExcludeFromCodeCoverage(Justification = ExcludeFromCoverageJustifications.RequiresUIThread)]
public sealed partial class GogSettingsPage : Page
{
    private readonly IGogSettingsViewModel _viewModel;

    public GogSettingsPage()
    {
        InitializeComponent();
        _viewModel = App.Current.Services.GetRequiredService<IGogSettingsViewModel>();
    }
}
