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
        PersistenceId = nameof(MainWindow);
    }

    public string Icon => "Assets/AppIcon.ico";
}
