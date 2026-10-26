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
        var state = new ParseState();
        foreach (string token in text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
        {
            ReadToken(token, state);
        }
        if (state.Words.Count > 0)
        {
            StripTrailingMarks(state);
        }
        if (state.Words.Count == 0)
        {
            throw new TaskParseException("The task needs a title: some words besides the #, @ and ! marks.");
        }
        if (state.DateCleared)
        {
            state.Due = null;
            state.Time = null;
        }
        else if (state.Due is null && state.Time is not null)
        {
            state.Due = _today;
        }
        return new ParsedTask(string.Join(' ', state.Words), state.Tags, state.Priority, state.Due, state.Time);
    }

    private void ReadToken(string token, ParseState state)
    {
        if (token.Length > 1 && token[0] == '#')
        {
            string tag = token[1..].ToLowerInvariant();
            if (!state.Tags.Contains(tag, StringComparer.Ordinal))
            {
                state.Tags.Add(tag);
            }
        }
        else if (token.Length > 1 && token[0] == '@')
        {
            ReadDateMark(token[1..], state);
        }
        else if (token is "!" or "!!")
        {
            state.Priority = Max(state.Priority, token.Length == 1 ? Priority.High : Priority.Urgent);
        }
        else
        {
            state.Words.Add(token);
        }
    }

    private static void StripTrailingMarks(ParseState state)
    {
        string last = state.Words[^1];
        int marks = 0;
        while (marks < 2 && last.Length - marks > 1 && last[^(marks + 1)] == '!')
        {
            marks++;
        }
        if (marks == 0)
        {
            return;
        }
        state.Words[^1] = last[..^marks];
        state.Priority = Max(state.Priority, marks == 1 ? Priority.High : Priority.Urgent);
    }

    private void ReadDateMark(string mark, ParseState state)
    {
        string lower = mark.ToLowerInvariant();
        if (lower == "none")
        {
            state.DateCleared = true;  // wins over every other date mark on the line, in any order
            return;
        }
        if (TimeOnly.TryParseExact(mark, ["H:mm", "HH:mm"], CultureInfo.InvariantCulture, DateTimeStyles.None, out TimeOnly parsedTime))
        {
            state.Time = parsedTime;
            return;
        }
        state.Due = ReadDay(lower) ?? throw new TaskParseException($"Unknown date mark @{mark}. Use @today, @tomorrow, a weekday like @fri, @2026-11-03, @+3d, @15:30 or @none.");
    }

    private static Priority Max(Priority a, Priority b) => a > b ? a : b;

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

    /// <summary>What has been read so far.</summary>
    private sealed class ParseState
    {
        public List<string> Words { get; } = [];

        public List<string> Tags { get; } = [];

        public Priority Priority { get; set; }

        public DateOnly? Due { get; set; }

        public TimeOnly? Time { get; set; }

        public bool DateCleared { get; set; }
    }
}
