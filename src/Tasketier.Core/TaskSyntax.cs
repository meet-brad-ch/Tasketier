using System.Globalization;

namespace Tasketier.Core;

/// <summary>Writes a task back as one line of inline syntax, so an edit starts from the full text.</summary>
public static class TaskSyntax
{
    /// <summary>The task as the user could have typed it: title, <c>@day</c>, <c>@time</c>, <c>@alarmN</c>, <c>!</c>, <c>#tags</c>.</summary>
    /// <param name="task">The task.</param>
    /// <returns>The line.</returns>
    public static string Render(TaskItem task)
    {
        ArgumentNullException.ThrowIfNull(task);
        var parts = new List<string> { task.Title };
        if (task.Due is DateOnly due)
        {
            parts.Add("@" + due.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
            if (task.DueTime is TimeOnly time)
            {
                parts.Add("@" + time.ToString("HH:mm", CultureInfo.InvariantCulture));
                if (task.AlarmMinutes is int minutes)
                {
                    parts.Add("@alarm" + minutes.ToString(CultureInfo.InvariantCulture));
                }
            }
        }
        switch (task.Priority)
        {
            case Priority.High:
                parts.Add("!");
                break;
            case Priority.Urgent:
                parts.Add("!!");
                break;
            case Priority.None:
                break;
        }
        parts.AddRange(task.Tags.Select(t => "#" + t));
        return string.Join(' ', parts);
    }

    /// <summary>A short due description: <c>due Fri 2026-10-30 15:30</c>.</summary>
    /// <param name="due">The day.</param>
    /// <param name="time">The time, or null.</param>
    /// <returns>The description.</returns>
    public static string DescribeDue(DateOnly due, TimeOnly? time)
    {
        string text = "due " + due.ToString("ddd yyyy-MM-dd", CultureInfo.InvariantCulture);
        return time is null ? text : text + " " + time.Value.ToString("HH:mm", CultureInfo.InvariantCulture);
    }
}
