namespace Tasketier.Core.Tests;

public sealed class TaskQueriesTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 26);
    private readonly TaskQueries _queries = new(Now);

    private static TaskItem Task(int id, string title, DateOnly? due = null, TimeOnly? time = null, Priority priority = Priority.None, DateOnly? planned = null, DateTimeOffset? completed = null, params string[] tags) => new()
    {
        Id = id,
        Title = title,
        Due = due,
        DueTime = time,
        Priority = priority,
        PlannedFor = planned,
        Completed = completed,
        Created = Now.AddMinutes(id),
        Tags = tags,
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
    public void Today_holds_overdue_due_today_and_planned_tasks_and_puts_overdue_first()
    {
        TaskDocument document = Document(
            Task(1, "due today", Today),
            Task(2, "overdue", Today.AddDays(-2)),
            Task(3, "tomorrow", Today.AddDays(1)),
            Task(4, "planned", planned: Today),
            Task(5, "carried over", planned: Today.AddDays(-3)),
            Task(6, "inbox"),
            Task(7, "done today", Today, completed: Now));

        int[] ids = [.. _queries.List(document, TaskView.Today).Select(t => t.Id)];

        Assert.Equal([2, 1, 4, 5], ids);
    }

    [Fact]
    public void Today_orders_by_time_then_priority_then_creation()
    {
        TaskDocument document = Document(
            Task(1, "late, no time", Today),
            Task(2, "afternoon", Today, new TimeOnly(15, 0)),
            Task(3, "morning", Today, new TimeOnly(9, 0)),
            Task(4, "urgent, no time", Today, priority: Priority.Urgent),
            Task(5, "high, no time", Today, priority: Priority.High));

        int[] ids = [.. _queries.List(document, TaskView.Today).Select(t => t.Id)];

        Assert.Equal([3, 2, 4, 5, 1], ids);
    }

    [Fact]
    public void Upcoming_holds_open_tasks_due_after_today_by_day()
    {
        TaskDocument document = Document(
            Task(1, "next week", Today.AddDays(7)),
            Task(2, "tomorrow", Today.AddDays(1)),
            Task(3, "today", Today),
            Task(4, "done later", Today.AddDays(2), completed: Now));

        int[] ids = [.. _queries.List(document, TaskView.Upcoming).Select(t => t.Id)];

        Assert.Equal([2, 1], ids);
    }

    [Fact]
    public void Inbox_holds_open_tasks_with_no_date_and_no_plan()
    {
        TaskDocument document = Document(
            Task(1, "bare"),
            Task(2, "planned", planned: Today),
            Task(3, "dated", Today.AddDays(3)),
            Task(4, "done bare", completed: Now));

        int[] ids = [.. _queries.List(document, TaskView.Inbox).Select(t => t.Id)];

        Assert.Equal([1], ids);
    }

    [Fact]
    public void All_holds_every_task_open_ones_first_then_done_ones_newest_first()
    {
        TaskDocument document = Document(
            Task(1, "undated"),
            Task(2, "next week", Today.AddDays(7)),
            Task(3, "overdue", Today.AddDays(-1)),
            Task(4, "today", Today),
            Task(5, "planned", planned: Today),
            Task(6, "done", completed: Now),
            Task(7, "done earlier", completed: Now.AddDays(-2)));

        int[] ids = [.. _queries.List(document, TaskView.All).Select(t => t.Id)];

        Assert.Equal([3, 4, 2, 1, 5, 6, 7], ids);
    }

    [Fact]
    public void Done_holds_the_last_thirty_days_newest_first()
    {
        TaskDocument document = Document(
            Task(1, "yesterday", completed: Now.AddDays(-1)),
            Task(2, "just now", completed: Now),
            Task(3, "long ago", completed: Now.AddDays(-31)),
            Task(4, "open"));

        int[] ids = [.. _queries.List(document, TaskView.Done).Select(t => t.Id)];

        Assert.Equal([2, 1], ids);
    }

    [Fact]
    public void A_filter_matches_the_title_or_a_tag_ignoring_case()
    {
        TaskDocument document = Document(
            Task(1, "Buy Milk"),
            Task(2, "Call", tags: "home"),
            Task(3, "Other"));

        Assert.Equal([1], _queries.List(document, TaskView.Inbox, "milk").Select(t => t.Id));
        Assert.Equal([2], _queries.List(document, TaskView.Inbox, "HOME").Select(t => t.Id));
        Assert.Equal([2], _queries.List(document, TaskView.Inbox, "#home").Select(t => t.Id));
        Assert.Empty(_queries.List(document, TaskView.Inbox, "nothing"));
    }

    [Fact]
    public void Search_lists_open_matches_first_then_done_ones()
    {
        TaskDocument document = Document(
            Task(1, "milk done", completed: Now),
            Task(2, "milk open"),
            Task(3, "bread"));

        Assert.Equal([2, 1], _queries.Search(document, "milk").Select(t => t.Id));
    }

    [Fact]
    public void IsOverdue_is_false_for_today_and_for_done_tasks()
    {
        Assert.False(_queries.IsOverdue(Task(1, "today", Today)));
        Assert.True(_queries.IsOverdue(Task(2, "yesterday", Today.AddDays(-1))));
        Assert.False(_queries.IsOverdue(Task(3, "yesterday done", Today.AddDays(-1), completed: Now)));
    }
}
