namespace Tasketier.Core;

/// <summary>The modifier keys of a hotkey, with the values Win32 <c>RegisterHotKey</c> uses.</summary>
[Flags]
public enum HotkeyModifiers
{
    /// <summary>No modifier.</summary>
    None = 0,

    /// <summary>Alt.</summary>
    Alt = 1,

    /// <summary>Ctrl.</summary>
    Control = 2,

    /// <summary>Shift.</summary>
    Shift = 4,

    /// <summary>The Windows key.</summary>
    Windows = 8,
}
