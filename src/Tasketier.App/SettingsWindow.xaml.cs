using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Win32;
using Tasketier.Core;

namespace Tasketier.App;

/// <summary>The settings dialog. <see cref="Result"/> holds the new settings after Save.</summary>
[SupportedOSPlatform("windows")]
public partial class SettingsWindow : Window
{
    private static readonly Key[] ModifierKeysOnly = [Key.LeftCtrl, Key.RightCtrl, Key.LeftAlt, Key.RightAlt, Key.LeftShift, Key.RightShift, Key.LWin, Key.RWin, Key.System];

    /// <summary>Creates the dialog from the current settings.</summary>
    /// <param name="current">The settings to show.</param>
    /// <param name="defaultFolder">The folder an empty data folder means.</param>
    public SettingsWindow(AppSettings current, string defaultFolder)
    {
        ArgumentNullException.ThrowIfNull(current);
        ArgumentNullException.ThrowIfNull(defaultFolder);
        InitializeComponent();
        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/Tasketier.ico"));
        CaptureBox.Text = current.CaptureHotkey;
        ListBox.Text = current.ListHotkey;
        FolderBox.Text = current.DataFolder;
        AutostartBox.IsChecked = current.StartWithWindows;
        FolderHint.Text = $"Empty means {defaultFolder}. Point it at a synced folder to carry the tasks between PCs; the file is tasks.json.";
    }

    /// <summary>The settings as saved, or null when the dialog was cancelled.</summary>
    public AppSettings? Result { get; private set; }

    private void OnHotkeyKeyDown(object sender, KeyEventArgs e)
    {
        e.Handled = true;
        Key key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (ModifierKeysOnly.Contains(key))
        {
            return;
        }
        HotkeyModifiers modifiers = HotkeyModifiers.None;
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            modifiers |= HotkeyModifiers.Control;
        }
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            modifiers |= HotkeyModifiers.Alt;
        }
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            modifiers |= HotkeyModifiers.Shift;
        }
        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Windows))
        {
            modifiers |= HotkeyModifiers.Windows;
        }
        if (modifiers == HotkeyModifiers.None)
        {
            Error.Text = "Hold Ctrl, Alt, Shift or Win while pressing the key.";
            return;
        }
        Error.Text = string.Empty;
        ((TextBox)sender).Text = new HotkeyChord(modifiers, key.ToString()).ToString();
    }

    private void OnBrowse(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = "Choose the folder for tasks.json" };
        if (FolderBox.Text.Length > 0 && Directory.Exists(FolderBox.Text))
        {
            dialog.InitialDirectory = FolderBox.Text;
        }
        if (dialog.ShowDialog(this) == true)
        {
            FolderBox.Text = dialog.FolderName;
        }
    }

    private void OnSave(object sender, RoutedEventArgs e)
    {
        try
        {
            HotkeyChord capture = HotkeyChord.Parse(CaptureBox.Text);
            HotkeyChord list = HotkeyChord.Parse(ListBox.Text);
            if (capture == list)
            {
                Error.Text = "The two hotkeys must differ.";
                return;
            }
        }
        catch (FormatException error)
        {
            Error.Text = error.Message;
            return;
        }
        string folder = FolderBox.Text.Trim();
        if (folder.Length > 0 && !Directory.Exists(folder))
        {
            Error.Text = $"The folder {folder} does not exist.";
            return;
        }
        Result = new AppSettings
        {
            CaptureHotkey = CaptureBox.Text,
            ListHotkey = ListBox.Text,
            DataFolder = folder,
            StartWithWindows = AutostartBox.IsChecked == true,
        };
        DialogResult = true;
    }
}
