using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Threading;
using Tasketier.App.Services;
using Tasketier.Core;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace Tasketier.App;

/// <summary>The Tasketier application: a tray icon, two global hotkeys, the capture box, the list window, the settings.</summary>
[SupportedOSPlatform("windows")]
public partial class App : Application, IDisposable
{
    /// <summary>Starts with the capture box open.</summary>
    public const string CaptureSwitch = "--capture";

    /// <summary>Starts with the list window open (the default without switches).</summary>
    public const string ListSwitch = "--list";

    /// <summary>Uses the folder that follows as the data folder instead of the settings' one; such an instance runs beside the normal one.</summary>
    public const string DataSwitch = "--data";

    private const int UndoCapacity = 20;
    private static readonly TimeSpan RefreshDelay = TimeSpan.FromMilliseconds(150);
    private static readonly TimeSpan AlarmPoll = TimeSpan.FromSeconds(30);

    private readonly UndoHistory _undo = new(UndoCapacity);
    private readonly ForegroundWindow _foreground = new();
    private readonly Dictionary<int, AlarmWindow> _openAlarms = [];
    private readonly Dictionary<int, DateTimeOffset> _snoozes = [];
    private DispatcherTimer? _alarmTimer;
    private Mutex? _instance;
    private InstanceSignal? _captureSignal;
    private InstanceSignal? _listSignal;
    private AppPaths? _paths;
    private SettingsStore? _settingsStore;
    private AppSettings? _settings;
    private TaskService? _service;
    private TaskFileWatcher? _watcher;
    private DispatcherTimer? _refreshTimer;
    private TrayIcon? _tray;
    private GlobalHotkey? _captureHotkey;
    private GlobalHotkey? _listHotkey;
    private ListWindow? _list;
    private CaptureWindow? _capture;

    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ArgumentNullException.ThrowIfNull(e);
        DispatcherUnhandledException += OnUnhandledException;
        try
        {
            Start(e.Args);
        }
#pragma warning disable CA1031 // the application boundary: every start-up failure becomes a visible message, then the app exits
        catch (Exception error)
#pragma warning restore CA1031
        {
            MessageBox.Show(error.Message, "Tasketier cannot start", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        Dispose();
        base.OnExit(e);
    }

    /// <summary>Releases the tray icon, the hotkeys, the timers, the watcher, the signals and the instance lock.</summary>
    public void Dispose()
    {
        DisposeHotkeysAndTray();
        DisposeTimersAndWatcher();
        DisposeSignalsAndLock();
        GC.SuppressFinalize(this);
    }

    private void DisposeHotkeysAndTray()
    {
        _captureHotkey?.Dispose();
        _captureHotkey = null;
        _listHotkey?.Dispose();
        _listHotkey = null;
        _tray?.Dispose();
        _tray = null;
    }

    private void DisposeTimersAndWatcher()
    {
        _refreshTimer?.Stop();
        _alarmTimer?.Stop();
        _watcher?.Dispose();
        _watcher = null;
    }

    private void DisposeSignalsAndLock()
    {
        _captureSignal?.Dispose();
        _captureSignal = null;
        _listSignal?.Dispose();
        _listSignal = null;
        _instance?.Dispose();
        _instance = null;
    }

    private void Start(string[] args)
    {
        string? dataOverride = DataFolderArgument(args);
        string scope = dataOverride is null ? string.Empty : "-" + Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(dataOverride.ToUpperInvariant())))[..16];
        _instance = new Mutex(true, "Local\\Tasketier-App" + scope, out bool first);
        _captureSignal = new InstanceSignal("capture" + scope);
        _listSignal = new InstanceSignal("list" + scope);
        if (!first)
        {
            // Another instance runs: hand it the request and leave.
            if (args.Contains(CaptureSwitch, StringComparer.Ordinal))
            {
                _captureSignal.Raise();
            }
            else if (!args.Contains(AutostartRegistration.TraySwitch, StringComparer.Ordinal))
            {
                _listSignal.Raise();
            }
            Shutdown();
            return;
        }
        _paths = new AppPaths();
        _settingsStore = new SettingsStore(_paths.SettingsFile);
        _settings = _settingsStore.Load();
        OpenStore(dataOverride ?? _paths.DataFolder(_settings));
        _tray = new TrayIcon(LoadIcon(), new TrayIcon.Actions(ShowCapture, ShowList, ShowSettings, ShowHelp, ExitApp));
        RegisterHotkeys();
        _captureSignal.Listen(() => Dispatcher.BeginInvoke(ShowCapture));
        _listSignal.Listen(() => Dispatcher.BeginInvoke(ShowList));
        UpdateTrayText();
        _alarmTimer = new DispatcherTimer { Interval = AlarmPoll };
        _alarmTimer.Tick += (_, _) => CheckAlarms();
        _alarmTimer.Start();
        CheckAlarms();
        if (args.Contains(CaptureSwitch, StringComparer.Ordinal))
        {
            ShowCapture();
        }
        else if (!args.Contains(AutostartRegistration.TraySwitch, StringComparer.Ordinal))
        {
            ShowList();
        }
    }

    private static string? DataFolderArgument(string[] args)
    {
        int index = Array.IndexOf(args, DataSwitch);
        if (index < 0)
        {
            return null;
        }
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"{DataSwitch} needs a folder: TasketierApp.exe {DataSwitch} D:\\tasks");
        }
        return Path.GetFullPath(args[index + 1]);
    }

    private void OpenStore(string folder)
    {
        _watcher?.Dispose();
        _service = new TaskService(new TaskStore(folder, TimeProvider.System), TimeProvider.System, _settings!.DefaultAlarmMinutes);
        _refreshTimer ??= MakeRefreshTimer();
        _watcher = new TaskFileWatcher(_service.Store);
        _watcher.Changed += (_, _) => Dispatcher.BeginInvoke(() =>
        {
            _refreshTimer.Stop();
            _refreshTimer.Start();
        });
    }

    private DispatcherTimer MakeRefreshTimer()
    {
        var timer = new DispatcherTimer { Interval = RefreshDelay };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            if (_list?.IsVisible == true)
            {
                _list.Refresh();
            }
            UpdateTrayText();
        };
        return timer;
    }

    private void RegisterHotkeys()
    {
        _captureHotkey?.Dispose();
        _captureHotkey = null;
        _listHotkey?.Dispose();
        _listHotkey = null;
        _captureHotkey = TryRegister(_settings!.CaptureHotkey, ShowCapture);
        _listHotkey = TryRegister(_settings.ListHotkey, ShowList);
    }

    private GlobalHotkey? TryRegister(string chordText, Action action)
    {
        try
        {
            var hotkey = new GlobalHotkey(HotkeyChord.Parse(chordText));
            hotkey.Pressed += (_, _) => action();
            return hotkey;
        }
        catch (Exception error) when (error is FormatException or HotkeyRegistrationException)
        {
            _tray?.Notify("Hotkey not registered", error.Message, isError: true);
            return null;
        }
    }

    private void ShowCapture()
    {
        if (_capture is not null)
        {
            _capture.Activate();
            return;
        }
        _foreground.Remember();
        _capture = new CaptureWindow(_service!, OnCaptured, _foreground);
        _capture.Closed += (_, _) => _capture = null;
        _capture.Show();
        _capture.Activate();
    }

    private void OnCaptured(TaskItem task)
    {
        _undo.Push(new UndoEntry($"added '{task.Title}'", task.Id, null));
        UpdateTrayText();
    }

    private void ShowList()
    {
        _list ??= new ListWindow(_service!, _undo, ShowHelp);
        _list.ShowAndFocus();
    }

    private void ShowHelp()
    {
        var help = new HelpWindow(_settings!);
        if (_list?.IsVisible == true)
        {
            help.Owner = _list;
        }
        else
        {
            help.WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
        help.Show();
    }

    private void ShowSettings()
    {
        var dialog = new SettingsWindow(_settings!, _paths!.DefaultFolder);
        if (dialog.ShowDialog() != true || dialog.Result is null)
        {
            return;
        }
        AppSettings previous = _settings!;
        _settings = dialog.Result;
        _settingsStore!.Save(_settings);
        RegisterHotkeys();
        new AutostartRegistration(Environment.ProcessPath!).Set(_settings.StartWithWindows);
        string folder = _paths.DataFolder(_settings);
        if (!string.Equals(folder, _paths.DataFolder(previous), StringComparison.OrdinalIgnoreCase) || _settings.DefaultAlarmMinutes != previous.DefaultAlarmMinutes)
        {
            OpenStore(folder);
            _list?.UseService(_service!);
        }
        UpdateTrayText();
    }

    private void UpdateTrayText()
    {
        if (_tray is null || _service is null)
        {
            return;
        }
        try
        {
            int open = _service.List(TaskView.Today).Count;
            _tray.SetText(open == 0 ? "Tasketier – nothing open today" : string.Format(CultureInfo.CurrentCulture, "Tasketier – {0} open today", open));
        }
        catch (TaskStoreDamagedException error)
        {
            _tray.SetText("Tasketier – the task file is damaged");
            _tray.Notify("Task file damaged", error.Message, isError: true);
        }
    }

    /// <summary>Shows a card for every alarm that is due, and for every snooze that ran out.</summary>
    private void CheckAlarms()
    {
        if (_service is null)
        {
            return;
        }
        try
        {
            foreach (TaskItem task in _service.DueAlarms())
            {
                _service.MarkAlarmFired(task.Id);
                ShowAlarm(task);
            }
            DateTimeOffset now = TimeProvider.System.GetLocalNow();
            foreach (int id in _snoozes.Where(s => s.Value <= now).Select(s => s.Key).ToList())
            {
                _snoozes.Remove(id);
                TaskItem task = _service.Get(id);
                if (!task.IsDone)
                {
                    ShowAlarm(task);
                }
            }
        }
        catch (Exception error) when (error is TaskStoreDamagedException or TaskNotFoundException or TimeoutException)
        {
            _tray?.Notify("Alarms", error.Message, isError: true);
        }
    }

    private void ShowAlarm(TaskItem task)
    {
        if (_openAlarms.ContainsKey(task.Id))
        {
            return;
        }
        var actions = new AlarmWindow.Actions(
            Done: () => Act(() =>
            {
                _undo.Push(new UndoEntry($"completed '{task.Title}'", task.Id, task.Clone()));
                _service!.Complete(task.Id);
            }),
            Snooze: minutes => _snoozes[task.Id] = TimeProvider.System.GetLocalNow().AddMinutes(minutes),
            Disarm: () => { });
        var card = new AlarmWindow(task, actions, _openAlarms.Count);
        _openAlarms[task.Id] = card;
        card.Closed += (_, _) => _openAlarms.Remove(task.Id);
        card.Show();
    }

    /// <summary>Runs a change from an alarm card; a known failure is shown on the tray, never swallowed.</summary>
    private void Act(Action change)
    {
        try
        {
            change();
        }
        catch (Exception error) when (error is TaskNotFoundException or TaskStoreDamagedException or TimeoutException)
        {
            _tray?.Notify("Tasketier", error.Message, isError: true);
        }
        UpdateTrayText();
    }

    private void ExitApp()
    {
        _list?.AllowClose();
        _list?.Close();
        Shutdown();
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Visible, never silent: the tray app keeps running so the hotkeys stay, the error is shown as is.
        MessageBox.Show(e.Exception.Message, "Tasketier error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static System.Drawing.Icon LoadIcon()
    {
        using Stream stream = GetResourceStream(new Uri("pack://application:,,,/Tasketier.ico"))!.Stream;
        return new System.Drawing.Icon(stream);
    }
}
