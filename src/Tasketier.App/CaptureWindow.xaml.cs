using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Tasketier.App.Services;
using Tasketier.Core;

namespace Tasketier.App;

/// <summary>The quick-capture box: one line, Enter saves, Esc cancels, the focus goes back to the previous window.</summary>
[SupportedOSPlatform("windows")]
public partial class CaptureWindow : Window
{
    private static readonly TimeSpan ConfirmationTime = TimeSpan.FromMilliseconds(600);
    private static readonly Brush ErrorBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0x3B, 0x3B));

    private readonly TaskService _service;
    private readonly Action<TaskItem> _onAdded;
    private readonly ForegroundWindow _foreground;
    private readonly Brush _previewBrush;
    private bool _closing;

    /// <summary>Creates the box.</summary>
    /// <param name="service">The service that stores the task.</param>
    /// <param name="onAdded">Runs after a task was stored, with the task.</param>
    /// <param name="foreground">The window to give the focus back to.</param>
    public CaptureWindow(TaskService service, Action<TaskItem> onAdded, ForegroundWindow foreground)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _onAdded = onAdded ?? throw new ArgumentNullException(nameof(onAdded));
        _foreground = foreground ?? throw new ArgumentNullException(nameof(foreground));
        InitializeComponent();
        _previewBrush = Preview.Foreground;
        HelpText.Text = HelpTexts.Syntax;
        ShowHint();
        Loaded += (_, _) =>
        {
            WindowPlacement.OnPointerMonitor(this);
            Input.Focus();
        };
        Deactivated += (_, _) => CloseOnce();
        Closed += (_, _) => _foreground.Restore();
    }

    private void OnTextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        string text = Input.Text;
        if (string.IsNullOrWhiteSpace(text))
        {
            ShowHint();
            return;
        }
        try
        {
            ShowPreview(_service.Preview(text).Describe(), isError: false);
        }
        catch (TaskParseException error)
        {
            ShowPreview(error.Message, isError: true);
        }
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                e.Handled = true;
                CloseOnce();
                break;
            case Key.Enter:
                e.Handled = true;
                Save();
                break;
            case Key.F1:
                e.Handled = true;
                HelpToggle.IsChecked = HelpToggle.IsChecked != true;
                break;
        }
    }

    private void Save()
    {
        if (string.IsNullOrWhiteSpace(Input.Text))
        {
            CloseOnce();
            return;
        }
        TaskItem task;
        try
        {
            task = _service.Add(Input.Text);
        }
        catch (TaskParseException error)
        {
            ShowPreview(error.Message, isError: true);
            return;
        }
        _onAdded(task);
        Input.IsReadOnly = true;
        ShowPreview($"Added #{task.Id} {task.Title}", isError: false);
        var timer = new DispatcherTimer { Interval = ConfirmationTime };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            CloseOnce();
        };
        timer.Start();
    }

    private void ShowHint() => ShowPreview("Type a task. The due day, time, priority and tags are read from the text.", isError: false);

    private void ShowPreview(string text, bool isError)
    {
        Preview.Text = text;
        Preview.Foreground = isError ? ErrorBrush : _previewBrush;
    }

    private void CloseOnce()
    {
        if (_closing)
        {
            return;
        }
        _closing = true;
        Close();
    }
}
