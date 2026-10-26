using System.Globalization;
using System.Text;
using Tasketier.Core;

namespace Tasketier.Cli;

/// <summary>Prints tasks as aligned text lines, one per task.</summary>
public sealed class TaskTable
{
    // "Mon 2026-10-26 15:30 !" is 22 characters; one space keeps it off the priority column.
    private const int DueWidth = 23;

    private readonly TaskQueries _queries;

    /// <summary>Creates the table for a moment in time, which decides what counts as overdue.</summary>
    /// <param name="queries">The queries of that moment.</param>
    public TaskTable(TaskQueries queries)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    /// <summary>One line per task: id, done box, due, priority, alarm (<c>a5</c> = 5 minutes before), title, tags.</summary>
    /// <param name="tasks">The tasks, in the order to print.</param>
    /// <returns>The lines, or "(no tasks)".</returns>
    public string Render(IReadOnlyList<TaskItem> tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        if (tasks.Count == 0)
        {
            return "(no tasks)" + Environment.NewLine;
        }
        int idWidth = tasks.Max(t => t.Id.ToString(CultureInfo.InvariantCulture).Length);
        var text = new StringBuilder();
        foreach (TaskItem task in tasks)
        {
            text.Append(task.Id.ToString(CultureInfo.InvariantCulture).PadLeft(idWidth));
            text.Append(task.IsDone ? "  [x]  " : "  [ ]  ");
            text.Append(Due(task).PadRight(DueWidth));
            text.Append(Priority(task).PadRight(4));
            text.Append(Alarm(task).PadRight(6));
            text.Append(task.Title);
            foreach (string tag in task.Tags)
            {
                text.Append("  #").Append(tag);
            }
            text.AppendLine();
        }
        return text.ToString();
    }

    /// <summary>One task on one line, for the result of a change: <c>#12 Buy milk</c>.</summary>
    /// <param name="task">The task.</param>
    /// <returns>The line.</returns>
    public static string OneLine(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return "#" + task.Id.ToString(CultureInfo.InvariantCulture) + " " + task.Title;
    }

    private string Due(TaskItem task)
    {
        if (task.Due is not DateOnly due)
        {
            return task.PlannedFor is null ? string.Empty : "planned";
        }
        string text = due.ToString("ddd yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (task.DueTime is TimeOnly time)
        {
            text += " " + time.ToString("HH:mm", CultureInfo.InvariantCulture);
        }
        return _queries.IsOverdue(task) ? text + " !" : text;
    }

    private static string Alarm(TaskItem task) => task.AlarmMinutes is int minutes ? "a" + minutes.ToString(CultureInfo.InvariantCulture) : string.Empty;

    private static string Priority(TaskItem task) => task.Priority switch
    {
        Core.Priority.High => "!",
        Core.Priority.Urgent => "!!",
        _ => string.Empty,
    };
}
