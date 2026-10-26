using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Tasketier.Core;

namespace Tasketier.App;

/// <summary>One task as the list window shows it: the item plus the display strings the XAML binds to.</summary>
public sealed class TaskRow : INotifyPropertyChanged
{
    private readonly TaskQueries _queries;
    private bool _isEditing;
    private string _editText = string.Empty;

    /// <summary>Wraps a task.</summary>
    /// <param name="task">The task.</param>
    /// <param name="queries">The queries of this moment, for the overdue mark.</param>
    public TaskRow(TaskItem task, TaskQueries queries)
    {
        Task = task ?? throw new ArgumentNullException(nameof(task));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>The task.</summary>
    public TaskItem Task { get; }

    /// <summary>The id.</summary>
    public int Id => Task.Id;

    /// <summary>The title.</summary>
    public string Title => Task.Title;

    /// <summary>Whether the task is done; bound two-way to the checkbox, the window reacts to the change.</summary>
    public bool IsDone => Task.IsDone;

    /// <summary>Whether the task is in Today's plan.</summary>
    public bool IsPlanned => Task.PlannedFor is not null;

    /// <summary>Whether the task is overdue, for the red due text.</summary>
    public bool IsOverdue => _queries.IsOverdue(Task);

    /// <summary>The due text: <c>Fri 30 Oct 15:30</c>, <c>today</c>, or empty.</summary>
    public string DueText
    {
        get
        {
            if (Task.Due is not DateOnly due)
            {
                return string.Empty;
            }
            string day = due == DateOnly.FromDateTime(DateTime.Today) ? "today" : due.ToString("ddd d MMM", CultureInfo.CurrentCulture);
            return Task.DueTime is TimeOnly time ? day + " " + time.ToString("HH:mm", CultureInfo.InvariantCulture) : day;
        }
    }

    /// <summary>The priority mark: <c>!</c>, <c>!!</c> or empty.</summary>
    public string PriorityText => Task.Priority switch
    {
        Priority.High => "!",
        Priority.Urgent => "!!",
        _ => string.Empty,
    };

    /// <summary>The alarm as <c>⏰5</c>, or empty.</summary>
    public string AlarmText => Task.AlarmMinutes is int minutes ? "⏰" + minutes.ToString(CultureInfo.InvariantCulture) : string.Empty;

    /// <summary>Whether the task has tags, for the second line of the row.</summary>
    public bool HasTags => Task.Tags.Count > 0;

    /// <summary>The tags as <c>#home #shop</c>.</summary>
    public string TagsText => string.Join(' ', Task.Tags.Select(t => "#" + t));

    /// <summary>The whole task as one line of inline syntax, the text an edit starts from.</summary>
    public string Syntax => TaskSyntax.Render(Task);

    /// <summary>Whether the row shows the edit box instead of the title.</summary>
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (_isEditing != value)
            {
                _isEditing = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>The text in the edit box.</summary>
    public string EditText
    {
        get => _editText;
        set
        {
            if (!string.Equals(_editText, value, StringComparison.Ordinal))
            {
                _editText = value;
                OnPropertyChanged();
            }
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
