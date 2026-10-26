namespace Tasketier.Core.Tests;

public sealed class TaskParserTests
{
    // 2026-10-26 is a Monday.
    private static readonly DateOnly Monday = new(2026, 10, 26);
    private readonly TaskParser _parser = new(Monday);

    [Fact]
    public void Plain_words_are_the_title_with_no_marks()
    {
        ParsedTask parsed = _parser.Parse("  Buy   milk ");

        Assert.Equal("Buy milk", parsed.Title);
        Assert.Empty(parsed.Tags);
        Assert.Equal(Priority.None, parsed.Priority);
        Assert.Null(parsed.Due);
        Assert.Null(parsed.DueTime);
    }

    [Fact]
    public void Tags_are_lower_case_unique_and_in_order()
    {
        ParsedTask parsed = _parser.Parse("Call Bob #Work #home #work");

        Assert.Equal("Call Bob", parsed.Title);
        Assert.Equal(["work", "home"], parsed.Tags);
    }

    [Theory]
    [InlineData("Pay rent !", Priority.High)]
    [InlineData("Pay rent !!", Priority.Urgent)]
    [InlineData("Pay rent!", Priority.High)]
    [InlineData("Pay rent!!", Priority.Urgent)]
    [InlineData("! Pay rent", Priority.High)]
    public void Exclamation_marks_set_the_priority(string text, Priority expected)
    {
        ParsedTask parsed = _parser.Parse(text);

        Assert.Equal("Pay rent", parsed.Title);
        Assert.Equal(expected, parsed.Priority);
    }

    [Fact]
    public void A_lone_exclamation_word_stays_a_word()
    {
        ParsedTask parsed = _parser.Parse("Hello!!!");

        Assert.Equal("Hello!", parsed.Title);
        Assert.Equal(Priority.Urgent, parsed.Priority);
    }

    [Theory]
    [InlineData("@today", 0)]
    [InlineData("@tod", 0)]
    [InlineData("@tomorrow", 1)]
    [InlineData("@tom", 1)]
    [InlineData("@mon", 7)]
    [InlineData("@monday", 7)]
    [InlineData("@tue", 1)]
    [InlineData("@fri", 4)]
    [InlineData("@Friday", 4)]
    [InlineData("@sun", 6)]
    [InlineData("@+3d", 3)]
    [InlineData("@+2w", 14)]
    [InlineData("@2026-11-03", 8)]
    public void Day_marks_resolve_against_today_and_a_weekday_is_never_today(string mark, int daysAhead)
    {
        ParsedTask parsed = _parser.Parse("Dentist " + mark);

        Assert.Equal("Dentist", parsed.Title);
        Assert.Equal(Monday.AddDays(daysAhead), parsed.Due);
        Assert.Null(parsed.DueTime);
    }

    [Fact]
    public void A_time_without_a_day_means_today()
    {
        ParsedTask parsed = _parser.Parse("Standup @9:30");

        Assert.Equal(Monday, parsed.Due);
        Assert.Equal(new TimeOnly(9, 30), parsed.DueTime);
    }

    [Fact]
    public void A_day_and_a_time_combine_in_any_order()
    {
        ParsedTask parsed = _parser.Parse("@15:00 Review @fri the plan");

        Assert.Equal("Review the plan", parsed.Title);
        Assert.Equal(Monday.AddDays(4), parsed.Due);
        Assert.Equal(new TimeOnly(15, 0), parsed.DueTime);
    }

    [Fact]
    public void None_clears_the_date_and_time()
    {
        ParsedTask parsed = _parser.Parse("Someday @fri @none @10:00");

        Assert.Null(parsed.Due);
        Assert.Null(parsed.DueTime);
    }

    [Fact]
    public void A_bare_alarm_means_the_default_minutes_and_a_number_sets_them()
    {
        Assert.Equal(3, _parser.Parse("Standup @9:00 @alarm").AlarmMinutes);
        Assert.Equal(10, _parser.Parse("Standup @9:00 @alarm10").AlarmMinutes);
        Assert.Equal(7, new TaskParser(Monday, 7).Parse("Standup @9:00 @ALARM").AlarmMinutes);
        Assert.Null(_parser.Parse("Standup @9:00").AlarmMinutes);
    }

    [Fact]
    public void An_alarm_without_a_due_time_is_an_error()
    {
        TaskParseException error = Assert.Throws<TaskParseException>(() => _parser.Parse("Standup @fri @alarm"));

        Assert.Contains("due time", error.Message, StringComparison.Ordinal);
        Assert.Throws<TaskParseException>(() => _parser.Parse("Standup @9:00 @none @alarm"));
    }

    [Theory]
    [InlineData("@alarmx")]
    [InlineData("@alarm-5")]
    [InlineData("@alarm1441")]
    public void A_bad_alarm_mark_is_an_error(string mark)
    {
        Assert.Throws<TaskParseException>(() => _parser.Parse("Standup @9:00 " + mark));
    }

    [Fact]
    public void Describe_names_the_alarm()
    {
        Assert.Equal("due Mon 2026-10-26 09:00 · alarm 10 min before", _parser.Parse("Standup @9:00 @alarm10").Describe());
    }

    [Fact]
    public void An_unknown_date_mark_is_an_error_that_names_the_mark()
    {
        TaskParseException error = Assert.Throws<TaskParseException>(() => _parser.Parse("Dentist @soon"));

        Assert.Contains("@soon", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("#tag !! @fri")]
    public void A_line_without_title_words_is_an_error(string text)
    {
        Assert.Throws<TaskParseException>(() => _parser.Parse(text));
    }

    [Fact]
    public void An_email_like_word_is_a_date_mark_error_not_a_title()
    {
        Assert.Throws<TaskParseException>(() => _parser.Parse("Mail @bob"));
    }

    [Fact]
    public void Describe_names_due_priority_and_tags()
    {
        ParsedTask parsed = _parser.Parse("Dentist @fri @15:30 ! #health");

        Assert.Equal("due Fri 2026-10-30 15:30 · high · #health", parsed.Describe());
    }

    [Fact]
    public void Describe_says_no_date_for_a_bare_task()
    {
        Assert.Equal("no date", _parser.Parse("Dentist").Describe());
    }
}
