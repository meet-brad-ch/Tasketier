namespace Tasketier.Core;

/// <summary>One task as stored in the JSON file.</summary>
public sealed class TaskItem
{
    /// <summary>The number the user and the CLI refer to the task by. Never reused.</summary>
    public int Id { get; set; }

    /// <summary>The text without the inline marks.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>The tags, lower case, without the <c>#</c>.</summary>
    public IReadOnlyList<string> Tags { get; set; } = [];

    /// <summary>The priority.</summary>
    public Priority Priority { get; set; }

    /// <summary>The day the task is due, or null.</summary>
    public DateOnly? Due { get; set; }

    /// <summary>The time of day the task is due, only with <see cref="Due"/>.</summary>
    public TimeOnly? DueTime { get; set; }

    /// <summary>The day the task was pulled into Today, or null.</summary>
    public DateOnly? PlannedFor { get; set; }

    /// <summary>Minutes before the due time at which the alarm fires, or null for no alarm. Only with <see cref="DueTime"/>.</summary>
    public int? AlarmMinutes { get; set; }

    /// <summary>When the alarm fired, or null while it has not. Cleared when the due time or the alarm changes.</summary>
    public DateTimeOffset? AlarmFired { get; set; }

    /// <summary>When a snoozed alarm comes back, or null when the alarm is not snoozed.</summary>
    public DateTimeOffset? SnoozedUntil { get; set; }

    /// <summary>When the task was added.</summary>
    public DateTimeOffset Created { get; set; }

    /// <summary>When the task was completed, or null while it is open.</summary>
    public DateTimeOffset? Completed { get; set; }

    /// <summary>Whether the task is completed.</summary>
    public bool IsDone => Completed is not null;

    /// <summary>A copy, for undo snapshots.</summary>
    /// <returns>A new item with the same values.</returns>
    public TaskItem Clone() => new()
    {
        Id = Id,
        Title = Title,
        Tags = [.. Tags],
        Priority = Priority,
        Due = Due,
        DueTime = DueTime,
        PlannedFor = PlannedFor,
        AlarmMinutes = AlarmMinutes,
        AlarmFired = AlarmFired,
        SnoozedUntil = SnoozedUntil,
        Created = Created,
        Completed = Completed,
    };
}
