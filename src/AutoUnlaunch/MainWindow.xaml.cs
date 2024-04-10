using Microsoft.UI.Xaml;
using Windows.ApplicationModel;
using WinUIEx;

namespace MrCapitalQ.AutoUnlaunch;

public sealed partial class MainWindow : WindowEx
{
    public MainWindow()
    {
        InitializeComponent();

        Title = Package.Current.DisplayName;
        ExtendsContentIntoTitleBar = true;
    }

    public string Icon => "Assets/AppIcon.ico";

    private void myButton_Click(object sender, RoutedEventArgs e)
    {
        myButton.Content = "Clicked";
    }
}
