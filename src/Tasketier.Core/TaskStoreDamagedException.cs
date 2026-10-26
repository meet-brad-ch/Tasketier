namespace Tasketier.Core;

/// <summary>The task file exists but is not readable. It is never replaced silently; the user decides.</summary>
public sealed class TaskStoreDamagedException : Exception
{
    /// <summary>Creates the exception.</summary>
    public TaskStoreDamagedException()
    {
    }

    /// <summary>Creates the exception with a message.</summary>
    /// <param name="message">The message.</param>
    public TaskStoreDamagedException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with a message and a cause.</summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The cause.</param>
    public TaskStoreDamagedException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates the exception for a file.</summary>
    /// <param name="path">The task file.</param>
    /// <param name="innerException">The JSON or IO error.</param>
    /// <returns>The exception, with a message that names the file and the backup folder.</returns>
    public static TaskStoreDamagedException ForFile(string path, Exception innerException)
    {
        ArgumentNullException.ThrowIfNull(innerException);
        string folder = Path.GetDirectoryName(path) ?? string.Empty;
        return new TaskStoreDamagedException(
            $"The task file {path} cannot be read: {innerException.Message} Daily backups are in {Path.Combine(folder, TaskStore.BackupFolderName)}. Restore one by hand, or move the damaged file away.",
            innerException);
    }
}
