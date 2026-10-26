namespace Tasketier.Core.Tests;

public sealed class TaskSyntaxTests
{
    [Fact]
    public void Render_writes_every_part_in_a_fixed_order()
    {
        var task = new TaskItem
        {
            Title = "Dentist",
            Tags = ["health", "me"],
            Priority = Priority.Urgent,
            Due = new DateOnly(2026, 10, 30),
            DueTime = new TimeOnly(15, 30),
        };

        Assert.Equal("Dentist @2026-10-30 @15:30 !! #health #me", TaskSyntax.Render(task));
    }

    [Fact]
    public void Render_writes_the_alarm_after_the_time()
    {
        var task = new TaskItem { Title = "Standup", Due = new DateOnly(2026, 10, 26), DueTime = new TimeOnly(9, 0), AlarmMinutes = 5 };

        Assert.Equal("Standup @2026-10-26 @09:00 @alarm5", TaskSyntax.Render(task));
    }

    [Fact]
    public void Render_of_a_bare_task_is_its_title()
    {
        Assert.Equal("Buy milk", TaskSyntax.Render(new TaskItem { Title = "Buy milk" }));
    }

    [Fact]
    public void Render_round_trips_through_the_parser()
    {
        var parser = new TaskParser(new DateOnly(2026, 10, 26));
        ParsedTask first = parser.Parse("Dentist @fri @15:30 ! #health");
        var task = new TaskItem { Title = first.Title, Tags = first.Tags, Priority = first.Priority, Due = first.Due, DueTime = first.DueTime };

        ParsedTask second = parser.Parse(TaskSyntax.Render(task));

        Assert.Equal(first, second with { Tags = first.Tags });
        Assert.Equal(first.Tags, second.Tags);
    }
}
