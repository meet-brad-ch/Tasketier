using System.Globalization;

namespace Tasketier.Core;

/// <summary>What <see cref="TaskParser"/> read out of one line of inline syntax.</summary>
/// <param name="Title">The words that were not marks, joined by single spaces.</param>
/// <param name="Tags">The <c>#tags</c>, lower case, in order of appearance, without duplicates.</param>
/// <param name="Priority">The <c>!</c> marks.</param>
/// <param name="Due">The <c>@day</c>, or null.</param>
/// <param name="DueTime">The <c>@hh:mm</c>, or null.</param>
/// <param name="AlarmMinutes">The <c>@alarm</c> minutes before the due time, or null.</param>
public sealed record ParsedTask(string Title, IReadOnlyList<string> Tags, Priority Priority, DateOnly? Due, TimeOnly? DueTime, int? AlarmMinutes)
{
    /// <summary>One line that says what was parsed, shown under the capture box.</summary>
    /// <returns>For example <c>due Fri 2026-10-30 15:30 · alarm 5 min before · high · #home</c>, or <c>no date</c>.</returns>
    public string Describe()
    {
        var parts = new List<string>();
        parts.Add(Due is null ? "no date" : TaskSyntax.DescribeDue(Due.Value, DueTime));
        if (AlarmMinutes is int minutes)
        {
            parts.Add(string.Format(CultureInfo.InvariantCulture, "alarm {0} min before", minutes));
        }
        if (Priority != Priority.None)
        {
            parts.Add(Priority.ToString().ToLowerInvariant());
        }
        if (Tags.Count > 0)
        {
            parts.Add(string.Join(' ', Tags.Select(t => "#" + t)));
        }
        return string.Join(" · ", parts);
    }
}
