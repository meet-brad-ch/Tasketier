namespace Tasketier.Core;

/// <summary>A hotkey as written in the settings: <c>Ctrl+Alt+Space</c>. The key name is a WPF <c>Key</c> name.</summary>
/// <param name="Modifiers">The modifiers; at least one.</param>
/// <param name="Key">The key name, for example <c>Space</c>, <c>T</c>, <c>F12</c>.</param>
public sealed record HotkeyChord(HotkeyModifiers Modifiers, string Key)
{
    /// <summary>Reads <c>Ctrl+Alt+Space</c>; modifier names are Ctrl, Alt, Shift, Win in any order and case.</summary>
    /// <param name="text">The chord.</param>
    /// <returns>The chord.</returns>
    /// <exception cref="FormatException">The text has no key, no modifier, or an unknown modifier.</exception>
    public static HotkeyChord Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        string[] parts = text.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
        {
            throw new FormatException($"A hotkey needs at least one modifier and a key, like Ctrl+Alt+Space; got '{text}'.");
        }
        HotkeyModifiers modifiers = HotkeyModifiers.None;
        for (int i = 0; i < parts.Length - 1; i++)
        {
            modifiers |= parts[i].ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => HotkeyModifiers.Control,
                "ALT" => HotkeyModifiers.Alt,
                "SHIFT" => HotkeyModifiers.Shift,
                "WIN" or "WINDOWS" => HotkeyModifiers.Windows,
                _ => throw new FormatException($"Unknown modifier '{parts[i]}' in '{text}'; use Ctrl, Alt, Shift or Win."),
            };
        }
        return new HotkeyChord(modifiers, parts[^1]);
    }

    /// <summary>The chord as the settings write it: <c>Ctrl+Alt+Space</c>.</summary>
    /// <returns>The text.</returns>
    public override string ToString()
    {
        var parts = new List<string>();
        if (Modifiers.HasFlag(HotkeyModifiers.Control))
        {
            parts.Add("Ctrl");
        }
        if (Modifiers.HasFlag(HotkeyModifiers.Alt))
        {
            parts.Add("Alt");
        }
        if (Modifiers.HasFlag(HotkeyModifiers.Shift))
        {
            parts.Add("Shift");
        }
        if (Modifiers.HasFlag(HotkeyModifiers.Windows))
        {
            parts.Add("Win");
        }
        parts.Add(Key);
        return string.Join('+', parts);
    }
}
