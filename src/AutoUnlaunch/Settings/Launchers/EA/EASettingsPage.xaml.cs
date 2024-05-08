using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Diagnostics.CodeAnalysis;

namespace MrCapitalQ.AutoUnlaunch.Settings.Launchers.EA;

[ExcludeFromCodeCoverage(Justification = ExcludeFromCoverageJustifications.RequiresUIThread)]
public sealed partial class EASettingsPage : Page
{
    private readonly IEASettingsViewModel _viewModel;

    public EASettingsPage()
    {
        InitializeComponent();
        _viewModel = App.Current.Services.GetRequiredService<IEASettingsViewModel>();
    }
}
