namespace Tasketier.Core.Tests;

public sealed class HotkeyChordTests
{
    [Theory]
    [InlineData("Ctrl+Alt+Space", HotkeyModifiers.Control | HotkeyModifiers.Alt, "Space")]
    [InlineData("ctrl + alt + t", HotkeyModifiers.Control | HotkeyModifiers.Alt, "t")]
    [InlineData("Win+Shift+F12", HotkeyModifiers.Windows | HotkeyModifiers.Shift, "F12")]
    [InlineData("Control+X", HotkeyModifiers.Control, "X")]
    public void Parse_reads_modifiers_in_any_order_and_case(string text, HotkeyModifiers modifiers, string key)
    {
        HotkeyChord chord = HotkeyChord.Parse(text);

        Assert.Equal(modifiers, chord.Modifiers);
        Assert.Equal(key, chord.Key);
    }

    [Theory]
    [InlineData("Space")]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Meta+Space")]
    public void Parse_rejects_a_chord_without_modifier_or_key_or_with_an_unknown_modifier(string text)
    {
        Assert.Throws<FormatException>(() => HotkeyChord.Parse(text));
    }

    [Fact]
    public void ToString_writes_the_canonical_form()
    {
        Assert.Equal("Ctrl+Alt+Shift+Win+Space", HotkeyChord.Parse("win+shift+alt+ctrl+Space").ToString());
    }
}
