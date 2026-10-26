namespace Tasketier.Core;

/// <summary>How urgent a task is. Written as <c>!</c> (high) or <c>!!</c> (urgent) in the inline syntax.</summary>
public enum Priority
{
    /// <summary>No priority mark.</summary>
    None = 0,

    /// <summary>One exclamation mark.</summary>
    High = 1,

    /// <summary>Two exclamation marks.</summary>
    Urgent = 2,
}
