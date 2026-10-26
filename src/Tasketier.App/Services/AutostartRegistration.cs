using System.Runtime.Versioning;
using Microsoft.Win32;

namespace Tasketier.App.Services;

/// <summary>The per-user "start with Windows" entry: a value under HKCU\...\CurrentVersion\Run that starts the app with <c>--tray</c>.</summary>
[SupportedOSPlatform("windows")]
public sealed class AutostartRegistration
{
    /// <summary>The switch that keeps a start-up launch in the tray.</summary>
    public const string TraySwitch = "--tray";

    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "Tasketier";
    private readonly string _executable;

    /// <summary>Creates the registration for an executable.</summary>
    /// <param name="executable">The full path of TasketierApp.exe.</param>
    public AutostartRegistration(string executable)
    {
        _executable = executable ?? throw new ArgumentNullException(nameof(executable));
    }

    /// <summary>Whether the entry exists and points at this executable.</summary>
    public bool IsEnabled
    {
        get
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKey);
            return key?.GetValue(ValueName) is string value && string.Equals(value, Command, StringComparison.OrdinalIgnoreCase);
        }
    }

    private string Command => $"\"{_executable}\" {TraySwitch}";

    /// <summary>Adds or removes the entry.</summary>
    /// <param name="enabled">True to start with Windows.</param>
    public void Set(bool enabled)
    {
        using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKey, writable: true);
        if (enabled)
        {
            key.SetValue(ValueName, Command, RegistryValueKind.String);
        }
        else
        {
            key.DeleteValue(ValueName, throwOnMissingValue: false);
        }
    }
}
