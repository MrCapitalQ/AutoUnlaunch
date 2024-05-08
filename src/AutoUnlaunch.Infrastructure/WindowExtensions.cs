using System.Runtime.InteropServices;

namespace MrCapitalQ.AutoUnlaunch.Launchers.Handlers;

internal static partial class WindowExtensions
{
    private const int SW_HIDE = 0;
    private const int SW_SHOWMINIMZED = 2;
    private const int SW_MINIMIZE = 6;

    public static bool HideWindow(this nint hWnd) => ShowWindow(hWnd, SW_HIDE);
    public static bool MinimizeWindow(this nint hWnd, bool activateWindow = false)
        => ShowWindow(hWnd, activateWindow ? SW_SHOWMINIMZED : SW_MINIMIZE);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool ShowWindow(nint hWnd, int nCmdShow);
}
