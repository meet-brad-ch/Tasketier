namespace Tasketier.Core.Tests;

public sealed class UndoHistoryTests : IDisposable
{
    private readonly TempFolder _folder = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero));
    private readonly TaskService _service;

    public UndoHistoryTests()
    {
        _service = new TaskService(new TaskStore(_folder.Path, _clock), _clock);
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public void Do_records_a_change_that_succeeded_with_the_id_it_returned()
    {
        var history = new UndoHistory(20);
        TaskItem task = _service.Add("Milk");

        history.Do("completed", task.Clone(), () => _service.Complete(task.Id));

        Assert.Equal(1, history.Count);
        Assert.Equal(task.Id, history.Pop()!.TaskId);
    }

    [Fact]
    public void Do_records_nothing_when_the_change_fails()
    {
        var history = new UndoHistory(20);
        TaskItem task = _service.Add("Milk");

        Assert.Throws<TaskParseException>(() => history.Do("edited", task.Clone(), () => _service.Edit(task.Id, "Milk @soon")));
        Assert.Throws<TaskNotFoundException>(() => history.Do("deleted", task.Clone(), () => _service.Delete(99)));

        Assert.Equal(0, history.Count);
    }

    [Fact]
    public void Undoing_a_completion_reopens_the_task()
    {
        var history = new UndoHistory(20);
        TaskItem task = _service.Add("Milk");
        history.Push(new UndoEntry("completed", task.Id, task.Clone()));
        _service.Complete(task.Id);

        UndoEntry? undone = history.Undo(_service);

        Assert.NotNull(undone);
        Assert.Equal("completed", undone.Description);
        Assert.False(_service.Get(task.Id).IsDone);
        Assert.Equal(0, history.Count);
    }

    [Fact]
    public void Undoing_a_delete_brings_the_task_back()
    {
        var history = new UndoHistory(20);
        TaskItem task = _service.Add("Milk @fri");
        history.Push(new UndoEntry("deleted", task.Id, task.Clone()));
        _service.Delete(task.Id);

        history.Undo(_service);

        Assert.Equal("Milk", _service.Get(task.Id).Title);
    }

    [Fact]
    public void Undoing_an_add_deletes_the_task()
    {
        var history = new UndoHistory(20);
        TaskItem task = _service.Add("Oops");
        history.Push(new UndoEntry("added", task.Id, null));

        history.Undo(_service);

        Assert.Throws<TaskNotFoundException>(() => _service.Get(task.Id));
    }

    [Fact]
    public void Undo_with_nothing_recorded_returns_null()
    {
        Assert.Null(new UndoHistory(3).Undo(_service));
    }

    [Fact]
    public void The_oldest_entry_falls_off_when_the_capacity_is_reached()
    {
        var history = new UndoHistory(2);
        history.Push(new UndoEntry("one", 1, null));
        history.Push(new UndoEntry("two", 2, null));
        history.Push(new UndoEntry("three", 3, null));

        Assert.Equal(2, history.Count);
        Assert.Equal("three", history.Pop()!.Description);
        Assert.Equal("two", history.Pop()!.Description);
        Assert.Null(history.Pop());
    }
}
