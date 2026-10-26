using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Windows.Input;
using System.Windows.Interop;
using Tasketier.Core;

namespace Tasketier.App.Services;

/// <summary>One system-wide hotkey, registered on a message-only window; <see cref="Pressed"/> fires on the UI thread.</summary>
[SupportedOSPlatform("windows")]
public sealed class GlobalHotkey : IDisposable
{
    private static int _nextId = 1;

    private readonly HwndSource _source;
    private readonly int _id;
    private bool _registered;

    /// <summary>Registers the chord.</summary>
    /// <param name="chord">The chord, for example <c>Ctrl+Alt+Space</c>.</param>
    /// <exception cref="HotkeyRegistrationException">The key name is unknown or the chord is taken.</exception>
    public GlobalHotkey(HotkeyChord chord)
    {
        ArgumentNullException.ThrowIfNull(chord);
        Chord = chord;
        _id = Interlocked.Increment(ref _nextId);
        _source = new HwndSource(new HwndSourceParameters("Tasketier hotkey") { WindowStyle = 0, ParentWindow = new nint(-3) });  // HWND_MESSAGE
        _source.AddHook(OnMessage);
        uint virtualKey = VirtualKeyOf(chord);
        if (!NativeMethods.RegisterHotKey(_source.Handle, _id, (uint)chord.Modifiers | NativeMethods.ModNoRepeat, virtualKey))
        {
            var cause = new Win32Exception(Marshal.GetLastWin32Error());
            _source.Dispose();
            throw new HotkeyRegistrationException($"The hotkey {chord} could not be registered: {cause.Message} Another program may hold it. Choose a different hotkey in Settings.", cause);
        }
        _registered = true;
    }

    /// <summary>The chord was pressed.</summary>
    public event EventHandler? Pressed;

    /// <summary>The registered chord.</summary>
    public HotkeyChord Chord { get; }

    /// <summary>Unregisters the chord.</summary>
    public void Dispose()
    {
        if (_registered)
        {
            NativeMethods.UnregisterHotKey(_source.Handle, _id);
            _registered = false;
        }
        _source.RemoveHook(OnMessage);
        _source.Dispose();
    }

    private static uint VirtualKeyOf(HotkeyChord chord)
    {
        if (!Enum.TryParse(chord.Key, ignoreCase: true, out Key key) || key == Key.None)
        {
            throw new HotkeyRegistrationException($"'{chord.Key}' in {chord} is not a key name. Use names like Space, T, F12, OemTilde.");
        }
        return (uint)KeyInterop.VirtualKeyFromKey(key);
    }

    private nint OnMessage(nint window, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == NativeMethods.WmHotkey && wParam.ToInt32() == _id)
        {
            handled = true;
            Pressed?.Invoke(this, EventArgs.Empty);
        }
        return 0;
    }
}
