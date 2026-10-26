using System.Globalization;

namespace Tasketier.Core;

/// <summary>
/// Reads one line of inline syntax: words are the title, <c>#tag</c> is a tag, <c>!</c>/<c>!!</c> is the
/// priority, <c>@today</c> <c>@tomorrow</c> <c>@fri</c> <c>@2026-11-03</c> <c>@+3d</c> set the due day,
/// <c>@15:30</c> the due time (today when no day is given), <c>@none</c> clears the date.
/// </summary>
public sealed class TaskParser
{
    private static readonly string[] DayNames = ["sunday", "monday", "tuesday", "wednesday", "thursday", "friday", "saturday"];

    private readonly DateOnly _today;

    /// <summary>Creates a parser that resolves relative days against a given day.</summary>
    /// <param name="today">The day <c>@today</c> means.</param>
    public TaskParser(DateOnly today)
    {
        _today = today;
    }

    /// <summary>Parses one line.</summary>
    /// <param name="text">The line as typed.</param>
    /// <returns>The parsed parts.</returns>
    /// <exception cref="TaskParseException">The title is empty or an <c>@</c> mark is unknown.</exception>
    public ParsedTask Parse(string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        var words = new List<string>();
        var tags = new List<string>();
        Priority priority = Priority.None;
        DateOnly? due = null;
        TimeOnly? time = null;
        bool dateCleared = false;
        foreach (string token in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            if (token.Length > 1 && token[0] == '#')
            {
                string tag = token[1..].ToLowerInvariant();
                if (!tags.Contains(tag, StringComparer.Ordinal))
                {
                    tags.Add(tag);
                }
            }
            else if (token.Length > 1 && token[0] == '@')
            {
                ReadDateMark(token[1..], ref due, ref time, ref dateCleared);
            }
            else if (token is "!" or "!!")
            {
                priority = Max(priority, token.Length == 1 ? Priority.High : Priority.Urgent);
            }
            else
            {
                words.Add(token);
            }
        }
        if (words.Count > 0)
        {
            StripTrailingMarks(words, ref priority);
        }
        if (words.Count == 0)
        {
            throw new TaskParseException("The task needs a title: some words besides the #, @ and ! marks.");
        }
        if (dateCleared)
        {
            due = null;
            time = null;
        }
        else if (due is null && time is not null)
        {
            due = _today;
        }
        return new ParsedTask(string.Join(' ', words), tags, priority, due, time);
    }

    private static Priority Max(Priority a, Priority b) => a > b ? a : b;

    private static void StripTrailingMarks(List<string> words, ref Priority priority)
    {
        string last = words[^1];
        int marks = 0;
        while (marks < 2 && last.Length - marks > 1 && last[^(marks + 1)] == '!')
        {
            marks++;
        }
        if (marks == 0)
        {
            return;
        }
        words[^1] = last[..^marks];
        priority = Max(priority, marks == 1 ? Priority.High : Priority.Urgent);
    }

    private void ReadDateMark(string mark, ref DateOnly? due, ref TimeOnly? time, ref bool dateCleared)
    {
        string lower = mark.ToLowerInvariant();
        if (lower == "none")
        {
            dateCleared = true;  // wins over every other date mark on the line, in any order
            return;
        }
        if (TimeOnly.TryParseExact(mark, ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsedTime))
        {
            time = parsedTime;
            return;
        }
        due = ReadDay(lower) ?? throw new TaskParseException($"Unknown date mark @{mark}. Use @today, @tomorrow, a weekday like @fri, @2026-11-03, @+3d, @15:30 or @none.");
    }

    private DateOnly? ReadDay(string lower)
    {
        switch (lower)
        {
            case "today" or "tod":
                return _today;
            case "tomorrow" or "tom":
                return _today.AddDays(1);
        }
        if (DateOnly.TryParseExact(lower, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day))
        {
            return day;
        }
        if (lower.Length >= 3 && lower[0] == '+' && int.TryParse(lower.AsSpan(1, lower.Length - 2), NumberStyles.None, CultureInfo.InvariantCulture, out int count))
        {
            return lower[^1] switch
            {
                'd' => _today.AddDays(count),
                'w' => _today.AddDays(7 * count),
                _ => null,
            };
        }
        return NextWeekday(lower);
    }

    private DateOnly? NextWeekday(string lower)
    {
        for (int i = 0; i < DayNames.Length; i++)
        {
            string name = DayNames[i];
            if (lower == name || (lower.Length == 3 && name.StartsWith(lower, StringComparison.Ordinal)))
            {
                int ahead = (i - (int)_today.DayOfWeek + 7) % 7;
                return _today.AddDays(ahead);
            }
        }
        return null;
    }
}
