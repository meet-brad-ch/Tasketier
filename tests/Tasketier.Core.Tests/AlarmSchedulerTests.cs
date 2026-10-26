namespace Tasketier.Core.Tests;

public sealed class AlarmSchedulerTests
{
    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;
    private static readonly DateOnly Day = new(2026, 10, 26);

    private static TaskItem Timed(int id, int hour, int minute, int? alarm, DateTimeOffset? fired = null, DateTimeOffset? completed = null) => new()
    {
        Id = id,
        Title = "t" + id,
        Due = Day,
        DueTime = new TimeOnly(hour, minute),
        AlarmMinutes = alarm,
        AlarmFired = fired,
        Completed = completed,
    };

    private static TaskDocument Document(params TaskItem[] tasks)
    {
        var document = new TaskDocument();
        foreach (TaskItem task in tasks)
        {
            document.Tasks.Add(task);
        }
        return document;
    }

    [Fact]
    public void The_alarm_instant_is_the_due_time_minus_the_lead()
    {
        DateTimeOffset? at = AlarmScheduler.AlarmInstant(Timed(1, 15, 30, 5), Utc);

        Assert.Equal(new DateTimeOffset(2026, 10, 26, 15, 25, 0, TimeSpan.Zero), at);
    }

    [Fact]
    public void A_task_without_time_or_without_alarm_has_no_instant()
    {
        Assert.Null(AlarmScheduler.AlarmInstant(Timed(1, 15, 30, null), Utc));
        Assert.Null(AlarmScheduler.AlarmInstant(new TaskItem { Id = 2, Title = "x", Due = Day, AlarmMinutes = 5 }, Utc));
    }

    [Fact]
    public void Due_lists_passed_unfired_alarms_of_open_tasks_earliest_first()
    {
        var now = new DateTimeOffset(2026, 10, 26, 15, 27, 0, TimeSpan.Zero);
        TaskDocument document = Document(
            Timed(1, 15, 30, 5),                                   // 15:25, due
            Timed(2, 15, 30, 1),                                   // 15:29, not yet
            Timed(3, 15, 20, 3),                                   // 15:17, due, earlier
            Timed(4, 15, 30, 5, fired: now),                       // already fired
            Timed(5, 15, 30, 5, completed: now),                   // done
            Timed(6, 15, 30, null));                               // no alarm

        int[] ids = [.. AlarmScheduler.Due(document, now, Utc).Select(t => t.Id)];

        Assert.Equal([3, 1], ids);
    }

    [Fact]
    public void An_alarm_more_than_an_hour_past_its_due_time_is_not_fired_late()
    {
        var now = new DateTimeOffset(2026, 10, 26, 17, 0, 0, TimeSpan.Zero);
        TaskDocument document = Document(
            Timed(1, 15, 30, 5),   // due 15:30, 90 min ago: skipped
            Timed(2, 16, 10, 5));  // due 16:10, 50 min ago: still fired

        Assert.Equal([2], AlarmScheduler.Due(document, now, Utc).Select(t => t.Id));
    }
}
