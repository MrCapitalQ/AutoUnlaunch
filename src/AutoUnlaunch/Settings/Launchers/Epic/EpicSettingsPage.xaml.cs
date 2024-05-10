using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Diagnostics.CodeAnalysis;

namespace MrCapitalQ.AutoUnlaunch.Settings.Launchers.Epic;

[ExcludeFromCodeCoverage(Justification = ExcludeFromCoverageJustifications.RequiresUIThread)]
public sealed partial class EpicSettingsPage : Page
{
    private readonly IEpicSettingsViewModel _viewModel;

    public EpicSettingsPage()
    {
        InitializeComponent();
        _viewModel = App.Current.Services.GetRequiredService<IEpicSettingsViewModel>();
    }
}
