using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using MrCapitalQ.AutoUnlaunch.Shared;
using System.Diagnostics.CodeAnalysis;

namespace MrCapitalQ.AutoUnlaunch;

[ExcludeFromCodeCoverage(Justification = ExcludeFromCoverageJustifications.RequiresUIThread)]
public sealed partial class LifetimeWindow : Window
{
    public LifetimeWindow()
    {
        InitializeComponent();

        TrayIcon.ToolTipText = Windows.ApplicationModel.Package.Current.DisplayName;
    }

    private void OpenApplicationCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => App.Current.ShowMainWindow();

    private void ExitApplicationCommand_ExecuteRequested(XamlUICommand sender, ExecuteRequestedEventArgs args)
        => App.Current.Exit();
}
