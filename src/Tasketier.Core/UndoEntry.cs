namespace Tasketier.Core;

/// <summary>One undoable change: the task as it was before, or null when the change created it.</summary>
/// <param name="Description">What was done, for the status line: "completed 'Buy milk'".</param>
/// <param name="TaskId">The task.</param>
/// <param name="Before">The task before the change; null when the change added it, so undo deletes it.</param>
public sealed record UndoEntry(string Description, int TaskId, TaskItem? Before);
