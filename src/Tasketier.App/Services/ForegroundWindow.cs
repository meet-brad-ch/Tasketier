using System.Runtime.Versioning;

namespace Tasketier.App.Services;

/// <summary>Remembers the window that had the focus before the capture box opened, and gives it back.</summary>
[SupportedOSPlatform("windows")]
public sealed class ForegroundWindow
{
    private nint _previous;

    /// <summary>Records the current foreground window.</summary>
    public void Remember() => _previous = NativeMethods.GetForegroundWindow();

    /// <summary>Gives the focus back to the remembered window, when it still exists.</summary>
    public void Restore()
    {
        if (_previous != 0 && NativeMethods.IsWindow(_previous))
        {
            NativeMethods.SetForegroundWindow(_previous);
        }
        _previous = 0;
    }
}
