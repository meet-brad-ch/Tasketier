namespace Tasketier.Core.Tests;

public sealed class TaskServiceTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 10, 26);
    private readonly TempFolder _folder = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero));
    private readonly TaskService _service;

    public TaskServiceTests()
    {
        _service = new TaskService(new TaskStore(_folder.Path, _clock), _clock);
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Add_stores_the_parsed_line_with_the_next_id_and_the_creation_time()
    {
        TaskItem first = _service.Add("Buy milk @tomorrow #home !");
        TaskItem second = _service.Add("Call Bob");

        Assert.Equal(1, first.Id);
        Assert.Equal(2, second.Id);
        Assert.Equal("Buy milk", first.Title);
        Assert.Equal(Today.AddDays(1), first.Due);
        Assert.Equal(["home"], first.Tags);
        Assert.Equal(Priority.High, first.Priority);
        Assert.Equal(_clock.Now, first.Created);
        Assert.Equal(["Buy milk", "Call Bob"], _service.Store.Read().Tasks.Select(t => t.Title));
    }

    [Fact]
    public void Add_rejects_an_empty_line_and_stores_nothing()
    {
        Assert.Throws<TaskParseException>(() => _service.Add("#onlytag"));

        Assert.Empty(_service.Store.Read().Tasks);
    }

    [Fact]
    public void Complete_and_uncomplete_toggle_the_completion_time()
    {
        int id = _service.Add("Milk").Id;

        TaskItem done = _service.Complete(id);
        Assert.Equal(_clock.Now, done.Completed);
        Assert.True(done.IsDone);

        TaskItem open = _service.Uncomplete(id);
        Assert.Null(open.Completed);
    }

    [Fact]
    public void Edit_replaces_every_parsed_part_from_the_new_line()
    {
        int id = _service.Add("Milk @fri #home !").Id;

        TaskItem edited = _service.Edit(id, "Oat milk #shop");

        Assert.Equal("Oat milk", edited.Title);
        Assert.Equal(["shop"], edited.Tags);
        Assert.Equal(Priority.None, edited.Priority);
        Assert.Null(edited.Due);
        Assert.Equal(1, edited.Id);
    }

    [Fact]
    public void Plan_puts_a_task_into_today_and_unplan_takes_it_out()
    {
        int id = _service.Add("Someday").Id;

        Assert.Equal(Today, _service.Plan(id).PlannedFor);
        Assert.Equal([id], _service.List(TaskView.Today).Select(t => t.Id));

        Assert.Null(_service.Unplan(id).PlannedFor);
        Assert.Empty(_service.List(TaskView.Today));
    }

    [Fact]
    public void Delete_removes_the_task_and_returns_it()
    {
        int id = _service.Add("Gone").Id;

        TaskItem removed = _service.Delete(id);

        Assert.Equal("Gone", removed.Title);
        Assert.Throws<TaskNotFoundException>(() => _service.Get(id));
    }

    [Fact]
    public void Operations_on_an_unknown_id_fail_with_the_id()
    {
        TaskNotFoundException error = Assert.Throws<TaskNotFoundException>(() => _service.Complete(42));

        Assert.Equal(42, error.Id);
        Assert.Contains("42", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Restore_puts_a_deleted_task_back_with_its_old_id()
    {
        TaskItem original = _service.Add("Keep me @fri");
        _service.Add("Other");
        _service.Delete(original.Id);

        _service.Restore(original);

        TaskItem back = _service.Get(original.Id);
        Assert.Equal("Keep me", back.Title);
        Assert.Equal(original.Due, back.Due);
        Assert.Equal(3, _service.Add("Next").Id);  // the id counter did not go backwards
    }

    [Fact]
    public void Restore_replaces_an_existing_task_in_place()
    {
        TaskItem original = _service.Add("Before");
        _service.Edit(original.Id, "After");

        _service.Restore(original);

        Assert.Equal("Before", _service.Get(original.Id).Title);
    }

    [Fact]
    public void List_and_search_use_the_views()
    {
        _service.Add("Milk @today");
        _service.Add("Bread @tomorrow");
        _service.Add("Idea");
        int doneId = _service.Add("Old milk").Id;
        _service.Complete(doneId);

        Assert.Equal(["Milk"], _service.List(TaskView.Today).Select(t => t.Title));
        Assert.Equal(["Bread"], _service.List(TaskView.Upcoming).Select(t => t.Title));
        Assert.Equal(["Idea"], _service.List(TaskView.Inbox).Select(t => t.Title));
        Assert.Equal(["Old milk"], _service.List(TaskView.Done).Select(t => t.Title));
        Assert.Equal(["Milk", "Old milk"], _service.Search("milk").Select(t => t.Title));
    }

    [Fact]
    public void Preview_parses_without_storing()
    {
        ParsedTask parsed = _service.Preview("Milk @fri");

        Assert.Equal(Today.AddDays(4), parsed.Due);
        Assert.Empty(_service.Store.Read().Tasks);
    }
}
