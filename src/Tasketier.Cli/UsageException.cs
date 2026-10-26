namespace Tasketier.Cli;

/// <summary>The arguments were not understood; the message says what to type instead.</summary>
public sealed class UsageException : Exception
{
    /// <summary>Creates the exception.</summary>
    public UsageException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">What was wrong.</param>
    public UsageException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and a cause.</summary>
    /// <param name="message">What was wrong.</param>
    /// <param name="innerException">The cause.</param>
    public UsageException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
