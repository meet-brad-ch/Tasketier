using System.Runtime.Versioning;
using System.Windows.Forms;

namespace Tasketier.App;

/// <summary>The notification-area icon and its menu. A left click opens the list.</summary>
[SupportedOSPlatform("windows")]
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _icon;
    private readonly ContextMenuStrip _menu;

    /// <summary>Creates and shows the icon.</summary>
    /// <param name="icon">The icon image.</param>
    /// <param name="actions">What the menu items do.</param>
    public TrayIcon(System.Drawing.Icon icon, Actions actions)
    {
        ArgumentNullException.ThrowIfNull(actions);
        _menu = new ContextMenuStrip();
        _menu.Items.Add("Add a task…", null, (_, _) => actions.Capture());
        _menu.Items.Add("Open the list", null, (_, _) => actions.OpenList());
        _menu.Items.Add("Settings…", null, (_, _) => actions.Settings());
        _menu.Items.Add("Help…", null, (_, _) => actions.Help());
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add("Exit", null, (_, _) => actions.Exit());
        _icon = new NotifyIcon
        {
            Icon = icon,
            Text = "Tasketier",
            ContextMenuStrip = _menu,
            Visible = true,
        };
        _icon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                actions.OpenList();
            }
        };
    }

    /// <summary>The menu's actions.</summary>
    /// <param name="Capture">Runs on the Add a task item.</param>
    /// <param name="OpenList">Runs on a left click and on the Open the list item.</param>
    /// <param name="Settings">Runs on the Settings item.</param>
    /// <param name="Help">Runs on the Help item.</param>
    /// <param name="Exit">Runs on the Exit item.</param>
    public sealed record Actions(Action Capture, Action OpenList, Action Settings, Action Help, Action Exit);

    /// <summary>Sets the tooltip, for the open-today count.</summary>
    /// <param name="text">The text; Windows shows at most 127 characters.</param>
    public void SetText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        _icon.Text = text.Length > 127 ? text[..127] : text;
    }

    /// <summary>Shows a balloon message for a few seconds.</summary>
    /// <param name="title">The title.</param>
    /// <param name="text">The text.</param>
    /// <param name="isError">True for the error icon, false for the information icon.</param>
    public void Notify(string title, string text, bool isError)
    {
        _icon.ShowBalloonTip(4000, title, text, isError ? ToolTipIcon.Error : ToolTipIcon.Info);
    }

    /// <summary>Removes the icon.</summary>
    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
    }
}
