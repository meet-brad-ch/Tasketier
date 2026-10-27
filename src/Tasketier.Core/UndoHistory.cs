namespace Tasketier.Core;

/// <summary>The last changes of this session, newest first, capped so the memory stays bounded.</summary>
public sealed class UndoHistory
{
    private readonly LinkedList<UndoEntry> _entries = new();
    private readonly int _capacity;

    /// <summary>Creates a stack that keeps at most a number of entries.</summary>
    /// <param name="capacity">How many changes can be undone.</param>
    public UndoHistory(int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        _capacity = capacity;
    }

    /// <summary>How many changes can be undone right now.</summary>
    public int Count => _entries.Count;

    /// <summary>Records a change; the oldest one falls off when the stack is full.</summary>
    /// <param name="entry">The change.</param>
    public void Push(UndoEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _entries.AddFirst(entry);
        while (_entries.Count > _capacity)
        {
            _entries.RemoveLast();
        }
    }

    /// <summary>Runs a change and records it for undo only when it succeeds, so a failed change leaves no entry.</summary>
    /// <param name="description">What the change does, for the status line.</param>
    /// <param name="before">The task before the change, or null when the change adds it.</param>
    /// <param name="change">The change; it returns the task it changed, added or removed.</param>
    /// <returns>What the change returned.</returns>
    public TaskItem Do(string description, TaskItem? before, Func<TaskItem> change)
    {
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(change);
        TaskItem result = change();
        Push(new UndoEntry(description, result.Id, before));
        return result;
    }

    /// <summary>Takes the newest change off the stack.</summary>
    /// <returns>The change, or null when there is none.</returns>
    public UndoEntry? Pop()
    {
        if (_entries.First is null)
        {
            return null;
        }
        UndoEntry entry = _entries.First.Value;
        _entries.RemoveFirst();
        return entry;
    }

    /// <summary>Undoes the newest change through the service.</summary>
    /// <param name="service">The service.</param>
    /// <returns>The change that was undone, or null when there was none.</returns>
    public UndoEntry? Undo(TaskService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        UndoEntry? entry = Pop();
        if (entry is null)
        {
            return null;
        }
        if (entry.Before is null)
        {
            service.Delete(entry.TaskId);
        }
        else
        {
            service.Restore(entry.Before);
        }
        return entry;
    }
}
