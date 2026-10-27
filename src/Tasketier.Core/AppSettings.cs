namespace Tasketier.Core;

/// <summary>The settings file: hotkeys, data folder, autostart.</summary>
public sealed class AppSettings
{
    /// <summary>The default capture hotkey.</summary>
    public const string DefaultCaptureHotkey = "Ctrl+Alt+A";

    /// <summary>The default list hotkey.</summary>
    public const string DefaultListHotkey = "Ctrl+Alt+L";

    /// <summary>The minutes a bare <c>@alarm</c> means until the user changes it.</summary>
    public const int DefaultAlarmMinutesValue = 3;

    /// <summary>The minutes before the due time a bare <c>@alarm</c> means.</summary>
    public int DefaultAlarmMinutes { get; set; } = DefaultAlarmMinutesValue;

    /// <summary>The hotkey that opens the capture box.</summary>
    public string CaptureHotkey { get; set; } = DefaultCaptureHotkey;

    /// <summary>The hotkey that opens the list window.</summary>
    public string ListHotkey { get; set; } = DefaultListHotkey;

    /// <summary>The folder that holds <c>tasks.json</c>; empty means the default folder.</summary>
    public string DataFolder { get; set; } = string.Empty;
}
