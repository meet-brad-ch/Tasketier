namespace Tasketier.Core;

/// <summary>The view queries over a document: which tasks a view shows and in which order.</summary>
public sealed class TaskQueries
{
    /// <summary>How long a completed task stays in the Done view.</summary>
    public static readonly TimeSpan DoneWindow = TimeSpan.FromDays(30);

    private readonly DateOnly _today;
    private readonly DateTimeOffset _now;

    /// <summary>Creates the queries for a moment in time.</summary>
    /// <param name="now">The current time; its local date is "today".</param>
    public TaskQueries(DateTimeOffset now)
    {
        _now = now;
        _today = DateOnly.FromDateTime(now.LocalDateTime);
    }

    /// <summary>The tasks a view shows, in the view's order, optionally narrowed by a filter.</summary>
    /// <param name="document">The document.</param>
    /// <param name="view">The view.</param>
    /// <param name="filter">Text that the title or a tag must contain (case-insensitive), or empty for all.</param>
    /// <returns>The tasks.</returns>
    public IReadOnlyList<TaskItem> List(TaskDocument document, TaskView view, string filter = "")
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(filter);
        IEnumerable<TaskItem> tasks = document.Tasks.Where(t => Matches(t, filter));
        return view switch
        {
            TaskView.Today => Sort(tasks.Where(IsToday)),
            TaskView.Upcoming => Sort(tasks.Where(t => !t.IsDone && t.Due > _today)),
            TaskView.Inbox => Sort(tasks.Where(t => !t.IsDone && t.Due is null && t.PlannedFor is null)),
            TaskView.Done => [.. tasks.Where(t => t.Completed is not null && _now - t.Completed.Value <= DoneWindow).OrderByDescending(t => t.Completed)],
            _ => throw new ArgumentOutOfRangeException(nameof(view), view, "Unknown view."),
        };
    }

    /// <summary>Every task, open first, whose title or tags contain the text.</summary>
    /// <param name="document">The document.</param>
    /// <param name="text">The text to look for, case-insensitive.</param>
    /// <returns>The tasks.</returns>
    public IReadOnlyList<TaskItem> Search(TaskDocument document, string text)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(text);
        return [.. Sort(document.Tasks.Where(t => !t.IsDone && Matches(t, text))), .. document.Tasks.Where(t => t.IsDone && Matches(t, text)).OrderByDescending(t => t.Completed)];
    }

    /// <summary>Whether an open task belongs to Today: overdue, due today, or planned for today or earlier (carry-over).</summary>
    /// <param name="task">The task.</param>
    /// <returns>True when the Today view shows it.</returns>
    public bool IsToday(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return !task.IsDone && (task.Due <= _today || task.PlannedFor <= _today);
    }

    /// <summary>Whether an open task is due before today.</summary>
    /// <param name="task">The task.</param>
    /// <returns>True when overdue.</returns>
    public bool IsOverdue(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return !task.IsDone && task.Due < _today;
    }

    private static bool Matches(TaskItem task, string filter)
    {
        if (filter.Length == 0)
        {
            return true;
        }
        string needle = filter.StartsWith('#') ? filter[1..] : filter;
        return task.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)
            || task.Tags.Any(t => t.Contains(needle, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Overdue first by day, then by due day, due time (none last), priority (urgent first), creation.</summary>
    private List<TaskItem> Sort(IEnumerable<TaskItem> tasks) =>
        [.. tasks
            .OrderBy(t => IsOverdue(t) ? 0 : 1)
            .ThenBy(t => t.Due ?? DateOnly.MaxValue)
            .ThenBy(t => t.DueTime ?? TimeOnly.MaxValue)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.Created)];
}
