using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Tasketier.App.Services;

/// <summary>The Win32 calls the app needs: global hotkeys and the foreground window.</summary>
[SupportedOSPlatform("windows")]
internal static partial class NativeMethods
{
    /// <summary>The window message a registered hotkey posts.</summary>
    public const int WmHotkey = 0x0312;

    /// <summary>RegisterHotKey flag: no repeated messages while the key is held.</summary>
    public const uint ModNoRepeat = 0x4000;

    [LibraryImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool RegisterHotKey(nint window, int id, uint modifiers, uint virtualKey);

    [LibraryImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnregisterHotKey(nint window, int id);

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    public static partial nint GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(nint window);

    [LibraryImport("user32.dll")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsWindow(nint window);
}
