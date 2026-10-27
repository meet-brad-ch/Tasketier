using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using Tasketier.Core;

namespace Tasketier.Cli;

/// <summary>The MCP tools. Every tool returns the affected task record(s) as JSON.</summary>
[McpServerToolType]
public sealed class TaskTools
{
    private const string SyntaxHelp = "Inline syntax: words are the title. #tag adds a tag. ! means high priority, !! means urgent. " +
        "@today, @tomorrow, a weekday like @fri (the next Friday after today), @2026-11-03 or @+3d set the due day. @15:30 sets the due time, today when no day is given. @none removes the date. @alarm sets an alarm at the user's default lead before the due time (3 minutes unless Settings changes it). @alarmN sets one N minutes before (N from 0 to 1440). An alarm needs a due time. The desktop app shows it.";

    private readonly TaskService _service;

    /// <summary>Creates the tools over a service.</summary>
    /// <param name="service">The service.</param>
    public TaskTools(TaskService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    /// <summary>Adds a task.</summary>
    /// <param name="text">The task line in inline syntax.</param>
    /// <returns>The stored task as JSON.</returns>
    [McpServerTool(Name = "add_task"), Description("Add a task from one line of text. " + SyntaxHelp + " Returns the stored task, with its id.")]
    public string AddTask([Description("The task text, for example 'Call the dentist @fri @15:30 ! #health'.")] string text) => Json(() => _service.Add(text));

    /// <summary>Lists a view.</summary>
    /// <param name="view">today, upcoming, inbox or done.</param>
    /// <param name="filter">Text the title or a tag must contain, or empty.</param>
    /// <returns>The tasks as a JSON array.</returns>
    [McpServerTool(Name = "list_tasks"), Description("List the tasks of one view, in the view's order. The views: 'today' = overdue first, then due today, then planned for today. 'upcoming' = due after today. 'inbox' = no date and not planned. 'done' = completed in the last 30 days. 'all' = every task: open ones first (overdue, then by due day, undated last), then done ones, newest first. Returns a JSON array of tasks.")]
    public string ListTasks(
        [Description("One of: today, upcoming, inbox, done, all. Default today.")] string view = "today",
        [Description("Only tasks whose title or tag contains this text (case-insensitive). Empty for all.")] string filter = "") =>
        Json(() => _service.List(ParseView(view), filter ?? string.Empty));

    /// <summary>Completes a task.</summary>
    /// <param name="id">The task id.</param>
    /// <returns>The task as JSON.</returns>
    [McpServerTool(Name = "complete_task"), Description("Mark a task done. Returns the task.")]
    public string CompleteTask([Description("The task id from list_tasks or add_task.")] int id) => Json(() => _service.Complete(id));

    /// <summary>Reopens a task.</summary>
    /// <param name="id">The task id.</param>
    /// <returns>The task as JSON.</returns>
    [McpServerTool(Name = "reopen_task"), Description("Reopen a completed task. Returns the task.")]
    public string ReopenTask([Description("The task id.")] int id) => Json(() => _service.Uncomplete(id));

    /// <summary>Replaces a task's text.</summary>
    /// <param name="id">The task id.</param>
    /// <param name="text">The whole new line in inline syntax.</param>
    /// <returns>The task as JSON.</returns>
    [McpServerTool(Name = "update_task"), Description("Replace the title, the tags, the priority, the due day, the due time and the alarm of a task from one line of text. Parts that are not in the text are cleared. The plan for today and the done state stay. " + SyntaxHelp + " Returns the task.")]
    public string UpdateTask([Description("The task id.")] int id, [Description("The whole new task line, for example 'Call the dentist @mon ! #health'.")] string text) => Json(() => _service.Edit(id, text));

    /// <summary>Deletes a task.</summary>
    /// <param name="id">The task id.</param>
    /// <returns>The removed task as JSON.</returns>
    [McpServerTool(Name = "delete_task"), Description("Delete a task. There is no undo. Returns the removed task.")]
    public string DeleteTask([Description("The task id.")] int id) => Json(() => _service.Delete(id));

    /// <summary>Plans or unplans a task for today.</summary>
    /// <param name="id">The task id.</param>
    /// <param name="planned">True to plan, false to unplan.</param>
    /// <returns>The task as JSON.</returns>
    [McpServerTool(Name = "plan_today"), Description("Put a task into the Today view without a due date (planned = true), or remove it from Today (planned = false). Returns the task.")]
    public string PlanToday([Description("The task id.")] int id, [Description("True to plan the task for today, false to unplan it. Default true.")] bool planned = true) =>
        Json(() => planned ? _service.Plan(id) : _service.Unplan(id));

    /// <summary>Searches every task.</summary>
    /// <param name="text">The text to look for.</param>
    /// <returns>The tasks as a JSON array, open first.</returns>
    [McpServerTool(Name = "search_tasks"), Description("Find every task, open or done, whose title or tag contains the text. The match ignores case. Open tasks come first. Returns a JSON array of tasks.")]
    public string SearchTasks([Description("The text to look for.")] string text) => Json(() => _service.Search(text));

    /// <summary>Shows one task.</summary>
    /// <param name="id">The task id.</param>
    /// <returns>The task as JSON.</returns>
    [McpServerTool(Name = "get_task"), Description("Get one task by id. Returns the task.")]
    public string GetTask([Description("The task id.")] int id) => Json(() => _service.Get(id));

    private static TaskView ParseView(string view) => (view ?? "today").ToUpperInvariant() switch
    {
        "TODAY" => TaskView.Today,
        "UPCOMING" => TaskView.Upcoming,
        "INBOX" => TaskView.Inbox,
        "DONE" => TaskView.Done,
        "ALL" => TaskView.All,
        _ => throw new ArgumentException($"Unknown view '{view}'. Use today, upcoming, inbox, done or all.", nameof(view)),
    };

    /// <summary>Runs an operation; a known failure becomes an <see cref="McpException"/>, whose message reaches the client.</summary>
    private static string Json<T>(Func<T> operation)
    {
        try
        {
            return JsonSerializer.Serialize(operation(), TaskJson.Options);
        }
        catch (Exception error) when (error is TaskParseException or TaskNotFoundException or TaskStoreDamagedException or ArgumentException or TimeoutException)
        {
            throw new McpException(error.Message, error);
        }
    }
}
