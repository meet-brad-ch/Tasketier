namespace Tasketier.Core;

/// <summary>The operations of the app, the CLI and the MCP server, each one read-modify-write on the store.</summary>
public sealed class TaskService
{
    private readonly TaskStore _store;
    private readonly TimeProvider _clock;
    private readonly int _defaultAlarmMinutes;

    /// <summary>Creates the service.</summary>
    /// <param name="store">The store.</param>
    /// <param name="clock">The clock; its local date is "today".</param>
    /// <param name="defaultAlarmMinutes">The minutes a bare <c>@alarm</c> means.</param>
    public TaskService(TaskStore store, TimeProvider clock, int defaultAlarmMinutes = AppSettings.DefaultAlarmMinutesValue)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        _defaultAlarmMinutes = defaultAlarmMinutes;
    }

    /// <summary>The store.</summary>
    public TaskStore Store => _store;

    /// <summary>Today, by the service's clock.</summary>
    public DateOnly Today => DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);

    /// <summary>Parses a line without storing anything, for the capture box's preview.</summary>
    /// <param name="text">The line.</param>
    /// <returns>The parsed parts.</returns>
    /// <exception cref="TaskParseException">The line cannot be read.</exception>
    public ParsedTask Preview(string text) => new TaskParser(Today, _defaultAlarmMinutes).Parse(text);

    /// <summary>Adds a task from one line of inline syntax.</summary>
    /// <param name="text">The line.</param>
    /// <returns>The stored task.</returns>
    /// <exception cref="TaskParseException">The line cannot be read.</exception>
    public TaskItem Add(string text)
    {
        ParsedTask parsed = Preview(text);
        return _store.Update(document =>
        {
            var task = new TaskItem { Id = document.NextId, Created = _clock.GetLocalNow() };
            document.NextId++;
            Apply(parsed, task);
            document.Tasks.Add(task);
            return task.Clone();
        });
    }

    /// <summary>Replaces a task's title, tags, priority and due from one line of inline syntax.</summary>
    /// <param name="id">The task.</param>
    /// <param name="text">The whole line, as <see cref="TaskSyntax.Render"/> writes it.</param>
    /// <returns>The task after the change.</returns>
    public TaskItem Edit(int id, string text)
    {
        ParsedTask parsed = Preview(text);
        return _store.Update(document =>
        {
            TaskItem task = document.Find(id);
            Apply(parsed, task);
            return task.Clone();
        });
    }

    /// <summary>Marks a task done now.</summary>
    /// <param name="id">The task.</param>
    /// <returns>The task after the change.</returns>
    public TaskItem Complete(int id) => Change(id, task => task.Completed = _clock.GetLocalNow());

    /// <summary>Reopens a done task.</summary>
    /// <param name="id">The task.</param>
    /// <returns>The task after the change.</returns>
    public TaskItem Uncomplete(int id) => Change(id, task => task.Completed = null);

    /// <summary>Pulls a task into Today without giving it a due date.</summary>
    /// <param name="id">The task.</param>
    /// <returns>The task after the change.</returns>
    public TaskItem Plan(int id) => Change(id, task => task.PlannedFor = Today);

    /// <summary>Takes a task out of Today's plan.</summary>
    /// <param name="id">The task.</param>
    /// <returns>The task after the change.</returns>
    public TaskItem Unplan(int id) => Change(id, task => task.PlannedFor = null);

    /// <summary>Removes a task.</summary>
    /// <param name="id">The task.</param>
    /// <returns>The removed task.</returns>
    public TaskItem Delete(int id) => _store.Update(document =>
    {
        TaskItem task = document.Find(id);
        document.Tasks.Remove(task);
        return task;
    });

    /// <summary>Puts a snapshot back: replaces the task with that id, or re-inserts it when it was deleted.</summary>
    /// <param name="snapshot">The task as it was.</param>
    /// <returns>The restored task.</returns>
    public TaskItem Restore(TaskItem snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return _store.Update(document =>
        {
            TaskItem copy = snapshot.Clone();
            int index = IndexOf(document, snapshot.Id);
            if (index < 0)
            {
                document.Tasks.Add(copy);
                document.NextId = Math.Max(document.NextId, copy.Id + 1);
            }
            else
            {
                document.Tasks[index] = copy;
            }
            return copy.Clone();
        });
    }

    /// <summary>One task.</summary>
    /// <param name="id">The task.</param>
    /// <returns>The task.</returns>
    /// <exception cref="TaskNotFoundException">No task has that id.</exception>
    public TaskItem Get(int id) => _store.Read().Find(id).Clone();

    /// <summary>The tasks of a view.</summary>
    /// <param name="view">The view.</param>
    /// <param name="filter">Text that the title or a tag must contain, or empty.</param>
    /// <returns>The tasks in the view's order.</returns>
    public IReadOnlyList<TaskItem> List(TaskView view, string filter = "") => Queries().List(_store.Read(), view, filter);

    /// <summary>Every task whose title or tags contain the text, open first.</summary>
    /// <param name="text">The text.</param>
    /// <returns>The tasks.</returns>
    public IReadOnlyList<TaskItem> Search(string text) => Queries().Search(_store.Read(), text);

    /// <summary>The open tasks whose alarm is due now and has not fired.</summary>
    /// <returns>The tasks, earliest alarm first.</returns>
    public IReadOnlyList<TaskItem> DueAlarms() => AlarmScheduler.Due(_store.Read(), _clock.GetLocalNow(), _clock.LocalTimeZone);

    /// <summary>Records that a task's alarm fired, so it does not fire again.</summary>
    /// <param name="id">The task.</param>
    /// <returns>The task after the change.</returns>
    public TaskItem MarkAlarmFired(int id) => Change(id, task => task.AlarmFired = _clock.GetLocalNow());

    /// <summary>The view queries at this moment.</summary>
    /// <returns>The queries.</returns>
    public TaskQueries Queries() => new(_clock.GetLocalNow());

    private static void Apply(ParsedTask parsed, TaskItem task)
    {
        bool alarmChanged = task.Due != parsed.Due || task.DueTime != parsed.DueTime || task.AlarmMinutes != parsed.AlarmMinutes;
        task.Title = parsed.Title;
        task.Tags = parsed.Tags;
        task.Priority = parsed.Priority;
        task.Due = parsed.Due;
        task.DueTime = parsed.Due is null ? null : parsed.DueTime;
        task.AlarmMinutes = task.DueTime is null ? null : parsed.AlarmMinutes;
        if (alarmChanged)
        {
            task.AlarmFired = null;  // a moved or changed alarm fires again
        }
    }

    private static int IndexOf(TaskDocument document, int id)
    {
        for (int i = 0; i < document.Tasks.Count; i++)
        {
            if (document.Tasks[i].Id == id)
            {
                return i;
            }
        }
        return -1;
    }

    private TaskItem Change(int id, Action<TaskItem> change) => _store.Update(document =>
    {
        TaskItem task = document.Find(id);
        change(task);
        return task.Clone();
    });
}
