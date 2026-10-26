using System.Globalization;

namespace Tasketier.Core;

/// <summary>An id names no task.</summary>
public sealed class TaskNotFoundException : Exception
{
    /// <summary>Creates the exception.</summary>
    public TaskNotFoundException()
    {
    }

    /// <summary>Creates the exception for an id.</summary>
    /// <param name="id">The id that was looked up.</param>
    public TaskNotFoundException(int id)
        : base(string.Format(CultureInfo.InvariantCulture, "There is no task with id {0}.", id))
    {
        Id = id;
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">The message.</param>
    public TaskNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and a cause.</summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public TaskNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>The id that was looked up, or zero.</summary>
    public int Id { get; }
}
