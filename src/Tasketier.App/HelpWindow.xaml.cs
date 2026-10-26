using System.Runtime.Versioning;
using System.Windows;
using Tasketier.Core;

namespace Tasketier.App;

/// <summary>The help window: the inline syntax, the keys of the list, the current hotkeys.</summary>
[SupportedOSPlatform("windows")]
public partial class HelpWindow : Window
{
    /// <summary>Creates the window.</summary>
    /// <param name="settings">The settings, for the current hotkeys.</param>
    public HelpWindow(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        InitializeComponent();
        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/Tasketier.ico"));
        SyntaxText.Text = HelpTexts.Syntax;
        ShortcutsText.Text = HelpTexts.Shortcuts;
        HotkeysText.Text = $"From any window: {settings.CaptureHotkey} opens the capture box, {settings.ListHotkey} the list. Both are changeable in Settings (tray menu).";
    }
}
