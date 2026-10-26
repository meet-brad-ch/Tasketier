using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Media;

namespace Tasketier.App.Services;

/// <summary>Puts a window on the monitor that holds the mouse pointer.</summary>
[SupportedOSPlatform("windows")]
public static class WindowPlacement
{
    /// <summary>Centres a window horizontally on the pointer's monitor, a third of the way down.</summary>
    /// <param name="window">The window; its size must be known (call after measure or with explicit Width/Height).</param>
    public static void OnPointerMonitor(Window window)
    {
        ArgumentNullException.ThrowIfNull(window);
        System.Drawing.Rectangle area = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;
        DpiScale dpi = VisualTreeHelper.GetDpi(window);
        double left = area.Left / dpi.DpiScaleX;
        double top = area.Top / dpi.DpiScaleY;
        double width = area.Width / dpi.DpiScaleX;
        double height = area.Height / dpi.DpiScaleY;
        window.Left = left + (width - window.Width) / 2;
        window.Top = top + (height - window.Height) / 3;
    }
}
