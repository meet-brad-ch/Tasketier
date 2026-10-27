using System.Globalization;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Tasketier.App.Services;
using Tasketier.Core;

namespace Tasketier.App;

/// <summary>The alarm card: the task, its due time, and Done / Snooze / Disarm. It takes the focus and beeps until it closes; Esc snoozes one minute, only Disarm acknowledges.</summary>
[SupportedOSPlatform("windows")]
public partial class AlarmWindow : Window, IDisposable
{
    private readonly Actions _actions;
    private System.Media.SoundPlayer? _player;
    private bool _answered;

    /// <summary>Creates the card for a task.</summary>
    /// <param name="task">The task.</param>
    /// <param name="actions">What the buttons do; each one also closes the card.</param>
    /// <param name="index">How many alarm cards are already open, for stacking.</param>
    public AlarmWindow(TaskItem task, Actions actions, int index)
    {
        ArgumentNullException.ThrowIfNull(task);
        _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        InitializeComponent();
        TaskId = task.Id;
        TitleText.Text = task.Title;
        Brush led = (Brush)FindResource("Led");
        Brush ghost = (Brush)FindResource("LedGhost");
        SevenSegmentDigit[] digits = [Digit(HourTens, led, ghost), Digit(HourOnes, led, ghost), Digit(MinuteTens, led, ghost), Digit(MinuteOnes, led, ghost)];
        if (task.Due is DateOnly due && task.DueTime is TimeOnly time)
        {
            DueLine.Text = due.ToString("ddd d MMM", CultureInfo.CurrentCulture);
            string hhmm = time.ToString("HHmm", CultureInfo.InvariantCulture);
            for (int i = 0; i < 4; i++)
            {
                digits[i].Show(hhmm[i]);
            }
        }
        Loaded += (_, _) =>
        {
            WindowPlacement.TopRightOfPointerMonitor(this, index);
            PlaySound();
            Activate();
        };
        Closed += (_, _) => Dispose();
    }

    /// <summary>The card's actions.</summary>
    /// <param name="Done">Completes the task.</param>
    /// <param name="Snooze">Shows the alarm again after the given minutes.</param>
    /// <param name="Disarm">Acknowledges the alarm: the card closes and does not come back.</param>
    public sealed record Actions(Action Done, Action<int> Snooze, Action Disarm);

    /// <summary>The task the card is for.</summary>
    public int TaskId { get; }

    /// <summary>Stops the sound and releases the player.</summary>
    public void Dispose()
    {
        StopSound();
        GC.SuppressFinalize(this);
    }

    private static SevenSegmentDigit Digit(System.Windows.Controls.Canvas host, Brush led, Brush ghost)
    {
        var digit = new SevenSegmentDigit(host.Width, led, ghost);
        host.Children.Add(digit);
        return digit;
    }

    private void OnSnoozeBar(object sender, RoutedEventArgs e) => Snooze(1);

    private void OnDone(object sender, RoutedEventArgs e) => Answer(_actions.Done);

    private void OnSnooze5(object sender, RoutedEventArgs e) => Snooze(5);

    private void OnSnooze15(object sender, RoutedEventArgs e) => Snooze(15);

    private void OnSnooze60(object sender, RoutedEventArgs e) => Snooze(60);

    private void OnDisarm(object sender, RoutedEventArgs e) => Answer(_actions.Disarm);

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            Snooze(1);
        }
    }

    private void Snooze(int minutes) => Answer(() => _actions.Snooze(minutes));

    private void Answer(Action action)
    {
        _answered = true;
        action();
        Close();
    }

    /// <summary>Only Disarm acknowledges: a close without an answer (Alt+F4, the app exits) snoozes one minute.</summary>
    /// <param name="e">The event data.</param>
    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_answered)
        {
            _answered = true;
            _actions.Snooze(1);
        }
        base.OnClosing(e);
    }

    /// <summary>Loops the bundled beeps until the card closes; they do not depend on the Windows sound scheme.</summary>
    private void PlaySound()
    {
        System.IO.Stream stream = Application.GetResourceStream(new Uri("pack://application:,,,/alarm.wav"))!.Stream;
        _player = new System.Media.SoundPlayer(stream);
        _player.PlayLooping();  // asynchronous: the card appears while the beeps sound
    }

    private void StopSound()
    {
        _player?.Stop();
        _player?.Stream?.Dispose();
        _player?.Dispose();
        _player = null;
    }
}
