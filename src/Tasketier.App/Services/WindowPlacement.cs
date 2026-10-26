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

    /// <summary>Puts a window in the top-right corner of the pointer's monitor, stacked below earlier ones.</summary>
    /// <param name="window">The window; its size must be known.</param>
    /// <param name="index">How many windows already sit in the corner.</param>
    public static void TopRightOfPointerMonitor(Window window, int index)
    {
        ArgumentNullException.ThrowIfNull(window);
        const double margin = 16;
        System.Drawing.Rectangle area = System.Windows.Forms.Screen.FromPoint(System.Windows.Forms.Cursor.Position).WorkingArea;
        DpiScale dpi = VisualTreeHelper.GetDpi(window);
        double right = area.Right / dpi.DpiScaleX;
        double top = area.Top / dpi.DpiScaleY;
        window.Left = right - window.Width - margin;
        window.Top = top + margin + index * (window.Height + 12);
    }
}
