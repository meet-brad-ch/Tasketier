namespace Tasketier.Core;

/// <summary>Decides which alarms are due at a moment. The app asks every half minute.</summary>
public static class AlarmScheduler
{
    /// <summary>An alarm whose due time passed longer ago than this is not fired any more (a late start).</summary>
    public static readonly TimeSpan LateStartWindow = TimeSpan.FromHours(1);

    /// <summary>The instant an alarm fires: the due day and time minus the lead, in the local zone.</summary>
    /// <param name="task">The task.</param>
    /// <param name="zone">The local time zone.</param>
    /// <returns>The instant, or null when the task has no due time or no alarm.</returns>
    public static DateTimeOffset? AlarmInstant(TaskItem task, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(task);
        ArgumentNullException.ThrowIfNull(zone);
        if (task.Due is not DateOnly due || task.DueTime is not TimeOnly time || task.AlarmMinutes is not int minutes)
        {
            return null;
        }
        return DueInstant(due, time, zone).AddMinutes(-minutes);
    }

    /// <summary>The open tasks whose alarm instant has passed, has not fired, and whose due time is less than an hour ago.</summary>
    /// <param name="document">The document.</param>
    /// <param name="now">The moment.</param>
    /// <param name="zone">The local time zone.</param>
    /// <returns>The tasks, earliest alarm first.</returns>
    public static IReadOnlyList<TaskItem> Due(TaskDocument document, DateTimeOffset now, TimeZoneInfo zone)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(zone);
        var due = new List<(DateTimeOffset At, TaskItem Task)>();
        foreach (TaskItem task in document.Tasks)
        {
            if (task.IsDone || task.AlarmFired is not null || AlarmInstant(task, zone) is not DateTimeOffset at || at > now)
            {
                continue;
            }
            DateTimeOffset dueAt = DueInstant(task.Due!.Value, task.DueTime!.Value, zone);
            if (now - dueAt <= LateStartWindow)
            {
                due.Add((at, task));
            }
        }
        return [.. due.OrderBy(d => d.At).Select(d => d.Task)];
    }

    private static DateTimeOffset DueInstant(DateOnly due, TimeOnly time, TimeZoneInfo zone)
    {
        var local = due.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(local, zone.GetUtcOffset(local));
    }
}
