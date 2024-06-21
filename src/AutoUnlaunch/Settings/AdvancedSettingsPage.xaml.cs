using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Diagnostics.CodeAnalysis;

namespace MrCapitalQ.AutoUnlaunch.Settings;

[ExcludeFromCodeCoverage(Justification = ExcludeFromCoverageJustifications.RequiresUIThread)]
public sealed partial class AdvancedSettingsPage : Page
{
    private readonly AdvancedSettingsViewModel _viewModel;

    public AdvancedSettingsPage()
    {
        InitializeComponent();
        _viewModel = App.Current.Services.GetRequiredService<AdvancedSettingsViewModel>();
    }
}
