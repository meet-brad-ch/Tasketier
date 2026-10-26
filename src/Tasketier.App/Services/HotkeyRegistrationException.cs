namespace Tasketier.App.Services;

/// <summary>A hotkey could not be registered: the chord is not a key name, or another program holds it.</summary>
public sealed class HotkeyRegistrationException : Exception
{
    /// <summary>Creates the exception.</summary>
    public HotkeyRegistrationException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">The message, naming the chord.</param>
    public HotkeyRegistrationException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and a cause.</summary>
    /// <param name="message">The message, naming the chord.</param>
    /// <param name="innerException">The cause.</param>
    public HotkeyRegistrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
