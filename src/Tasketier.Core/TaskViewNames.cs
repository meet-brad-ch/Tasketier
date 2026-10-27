namespace Tasketier.Core;

/// <summary>The names of the views, as the CLI and the MCP server take them.</summary>
public static class TaskViewNames
{
    /// <summary>The names, for messages.</summary>
    public const string Choices = "today, upcoming, inbox, done or all";

    private static readonly Dictionary<string, TaskView> Views = new(StringComparer.OrdinalIgnoreCase)
    {
        ["today"] = TaskView.Today,
        ["upcoming"] = TaskView.Upcoming,
        ["inbox"] = TaskView.Inbox,
        ["done"] = TaskView.Done,
        ["all"] = TaskView.All,
    };

    /// <summary>Finds the view of a name, ignoring case.</summary>
    /// <param name="name">The name.</param>
    /// <param name="view">The view, when the name is known.</param>
    /// <returns>True when the name is known.</returns>
    public static bool TryParse(string? name, out TaskView view) => Views.TryGetValue(name ?? string.Empty, out view);
}
