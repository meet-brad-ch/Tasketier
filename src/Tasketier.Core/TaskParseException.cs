namespace Tasketier.Core;

/// <summary>A line of inline syntax could not be read: an empty title or an unknown <c>@</c> mark.</summary>
public sealed class TaskParseException : Exception
{
    /// <summary>Creates the exception.</summary>
    public TaskParseException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">What was wrong, for the user.</param>
    public TaskParseException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and a cause.</summary>
    /// <param name="message">What was wrong, for the user.</param>
    /// <param name="innerException">The cause.</param>
    public TaskParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
