namespace Tasketier.Core;

/// <summary>The four lists of the list window and of <c>tasketier list --view</c>.</summary>
public enum TaskView
{
    /// <summary>Open tasks that are overdue, due today or planned for today.</summary>
    Today = 0,

    /// <summary>Open tasks due after today.</summary>
    Upcoming = 1,

    /// <summary>Open tasks with no due date that are not planned.</summary>
    Inbox = 2,

    /// <summary>Tasks completed in the last thirty days.</summary>
    Done = 3,

    /// <summary>Every task: the open ones first (overdue, then by due day, undated last), then the done ones, newest first.</summary>
    All = 4,
}
