namespace Tasketier.Core;

/// <summary>The whole JSON file: a format version, the next free id and the tasks.</summary>
public sealed class TaskDocument
{
    /// <summary>The format version this library writes.</summary>
    public const int CurrentVersion = 1;

    /// <summary>The file format version.</summary>
    public int Version { get; set; } = CurrentVersion;

    /// <summary>The id the next added task gets.</summary>
    public int NextId { get; set; } = 1;

    /// <summary>The tasks, open and done, in creation order.</summary>
    public IList<TaskItem> Tasks { get; init; } = [];

    /// <summary>Finds a task by id.</summary>
    /// <param name="id">The id.</param>
    /// <returns>The task.</returns>
    /// <exception cref="TaskNotFoundException">No task has that id.</exception>
    public TaskItem Find(int id) => Tasks.FirstOrDefault(t => t.Id == id) ?? throw new TaskNotFoundException(id);
}
