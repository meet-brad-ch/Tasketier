namespace Tasketier.Core;

/// <summary>The settings file exists but is not readable.</summary>
public sealed class SettingsDamagedException : Exception
{
    /// <summary>Creates the exception.</summary>
    public SettingsDamagedException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">The message.</param>
    public SettingsDamagedException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and a cause.</summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public SettingsDamagedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
