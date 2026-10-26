using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tasketier.Core;

namespace Tasketier.App;

/// <summary>The list window: four views, a filter, an add box, inline edit, plan star, undo.</summary>
[SupportedOSPlatform("windows")]
public partial class ListWindow : Window
{
    private readonly UndoHistory _undo;
    private readonly Action _showHelp;
    private readonly ObservableCollection<TaskRow> _rows = [];
    private TaskService _service;
    private bool _allowClose;

    /// <summary>Creates the window.</summary>
    /// <param name="service">The service.</param>
    /// <param name="undo">The session's undo history, shared with the capture box.</param>
    /// <param name="showHelp">Opens the help window.</param>
    public ListWindow(TaskService service, UndoHistory undo, Action showHelp)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _undo = undo ?? throw new ArgumentNullException(nameof(undo));
        _showHelp = showHelp ?? throw new ArgumentNullException(nameof(showHelp));
        InitializeComponent();
        Tasks.ItemsSource = _rows;
        Views.SelectedIndex = 0;
        Icon = System.Windows.Media.Imaging.BitmapFrame.Create(new Uri("pack://application:,,,/Tasketier.ico"));
    }

    /// <summary>The view the window shows, named by the selected rail item's Tag.</summary>
    public TaskView View => Views.SelectedItem is ListBoxItem { Tag: string name } ? Enum.Parse<TaskView>(name) : TaskView.Today;

    /// <summary>Points the window at another service (after the data folder changed) and refreshes.</summary>
    /// <param name="service">The new service.</param>
    public void UseService(TaskService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        Refresh();
    }

    /// <summary>Reads the current view again from the store; keeps the selection by id.</summary>
    public void Refresh()
    {
        int? selectedId = (Tasks.SelectedItem as TaskRow)?.Id;
        TaskQueries queries = _service.Queries();
        IReadOnlyList<TaskItem> tasks;
        try
        {
            tasks = _service.List(View, FilterBox.Text.Trim());
        }
        catch (TaskStoreDamagedException error)
        {
            _rows.Clear();
            Status.Text = error.Message;
            return;
        }
        _rows.Clear();
        foreach (TaskItem task in tasks)
        {
            _rows.Add(new TaskRow(task, queries));
        }
        if (selectedId is int id)
        {
            Tasks.SelectedItem = _rows.FirstOrDefault(r => r.Id == id);
        }
    }

    /// <summary>Shows the window, in front, with the add box focused.</summary>
    public void ShowAndFocus()
    {
        Refresh();
        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }
        Show();
        Activate();
        AddBox.Focus();
    }

    /// <summary>Lets the next close really close the window, for the app's exit.</summary>
    public void AllowClose() => _allowClose = true;

    /// <inheritdoc />
    protected override void OnClosing(CancelEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (!_allowClose)
        {
            e.Cancel = true;  // a tray app: closing hides
            Hide();
        }
        base.OnClosing(e);
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);
        if (e.Key == Key.Z && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            Undo();
        }
        else if (e.Key == Key.F && Keyboard.Modifiers == ModifierKeys.Control)
        {
            e.Handled = true;
            FilterBox.Focus();
            FilterBox.SelectAll();
        }
        else if (e.Key == Key.F1)
        {
            e.Handled = true;
            _showHelp();
        }
        else if (e.Key == Key.Escape && !_rows.Any(r => r.IsEditing))
        {
            e.Handled = true;
            Hide();
        }
        base.OnPreviewKeyDown(e);
    }

    private void OnViewChanged(object sender, SelectionChangedEventArgs e) => Refresh();

    private void OnHelpClick(object sender, RoutedEventArgs e) => _showHelp();

    private void OnFilterChanged(object sender, TextChangedEventArgs e)
    {
        FilterHint.Visibility = FilterBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        Refresh();
    }

    private void OnFilterKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && FilterBox.Text.Length > 0)
        {
            e.Handled = true;
            FilterBox.Clear();
        }
    }

    private void OnAddTextChanged(object sender, TextChangedEventArgs e) =>
        AddHint.Visibility = AddBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void OnAddKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || string.IsNullOrWhiteSpace(AddBox.Text))
        {
            return;
        }
        e.Handled = true;
        Act(() =>
        {
            TaskItem task = _service.Add(AddBox.Text);
            _undo.Push(new UndoEntry($"added '{task.Title}'", task.Id, null));
            AddBox.Clear();
            return $"Added #{task.Id} {task.Title}";
        });
    }

    private void OnDoneClick(object sender, RoutedEventArgs e)
    {
        if (RowOf(sender) is not TaskRow row)
        {
            return;
        }
        Act(() =>
        {
            if (row.IsDone)
            {
                _undo.Push(new UndoEntry($"reopened '{row.Title}'", row.Id, row.Task.Clone()));
                _service.Uncomplete(row.Id);
                return $"Reopened #{row.Id} {row.Title}";
            }
            _undo.Push(new UndoEntry($"completed '{row.Title}'", row.Id, row.Task.Clone()));
            _service.Complete(row.Id);
            return $"Completed #{row.Id} {row.Title}";
        });
    }

    private void OnPlanClick(object sender, RoutedEventArgs e)
    {
        if (RowOf(sender) is not TaskRow row)
        {
            return;
        }
        Act(() =>
        {
            _undo.Push(new UndoEntry(row.IsPlanned ? $"unplanned '{row.Title}'" : $"planned '{row.Title}'", row.Id, row.Task.Clone()));
            if (row.IsPlanned)
            {
                _service.Unplan(row.Id);
                return $"Taken out of today: #{row.Id} {row.Title}";
            }
            _service.Plan(row.Id);
            return $"Planned for today: #{row.Id} {row.Title}";
        });
    }

    private void OnTitleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 2 && RowOf(sender) is TaskRow row)
        {
            e.Handled = true;
            BeginEdit(row);
        }
    }

    private void OnTasksKeyDown(object sender, KeyEventArgs e)
    {
        if (Tasks.SelectedItem is not TaskRow row || row.IsEditing)
        {
            return;
        }
        switch (e.Key)
        {
            case Key.F2:
                e.Handled = true;
                BeginEdit(row);
                break;
            case Key.Delete:
                e.Handled = true;
                Act(() =>
                {
                    _undo.Push(new UndoEntry($"deleted '{row.Title}'", row.Id, row.Task.Clone()));
                    _service.Delete(row.Id);
                    return $"Deleted #{row.Id} {row.Title} (Ctrl+Z brings it back)";
                });
                break;
            case Key.Space:
                e.Handled = true;
                OnDoneClick(Tasks.ItemContainerGenerator.ContainerFromItem(row) ?? sender, e);
                break;
        }
    }

    private void OnEditLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox box && box.Visibility == Visibility.Visible)
        {
            box.Focus();
            box.SelectAll();
        }
    }

    private void OnEditKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox box || box.DataContext is not TaskRow row)
        {
            return;
        }
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            CommitEdit(row);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            row.IsEditing = false;
            Tasks.Focus();
        }
    }

    private void OnEditLostFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBox { DataContext: TaskRow { IsEditing: true } row })
        {
            CommitEdit(row);
        }
    }

    private void BeginEdit(TaskRow row)
    {
        row.EditText = row.Syntax;
        row.IsEditing = true;
        if (Tasks.ItemContainerGenerator.ContainerFromItem(row) is ListViewItem item && FindEditBox(item) is TextBox box)
        {
            box.Focus();
            box.SelectAll();
        }
    }

    private void CommitEdit(TaskRow row)
    {
        row.IsEditing = false;
        string text = row.EditText.Trim();
        if (text.Length == 0 || string.Equals(text, row.Syntax, StringComparison.Ordinal))
        {
            return;
        }
        Act(() =>
        {
            _undo.Push(new UndoEntry($"edited '{row.Title}'", row.Id, row.Task.Clone()));
            TaskItem edited = _service.Edit(row.Id, text);
            return $"Edited #{edited.Id} {edited.Title}";
        });
    }

    private void Undo()
    {
        Act(() =>
        {
            UndoEntry? entry = _undo.Undo(_service);
            return entry is null ? "Nothing to undo" : "Undid: " + entry.Description;
        });
    }

    /// <summary>Runs a change, shows its result or its error in the status line, and refreshes.</summary>
    private void Act(Func<string> change)
    {
        try
        {
            Status.Text = change();
        }
        catch (Exception error) when (error is TaskParseException or TaskNotFoundException or TaskStoreDamagedException or TimeoutException)
        {
            Status.Text = error.Message;
        }
        Refresh();
    }

    private static TaskRow? RowOf(object sender) => (sender as FrameworkElement)?.DataContext as TaskRow;

    private static TextBox? FindEditBox(DependencyObject parent)
    {
        int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
            if (child is TextBox box)
            {
                return box;
            }
            if (FindEditBox(child) is TextBox inner)
            {
                return inner;
            }
        }
        return null;
    }
}
