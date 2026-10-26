namespace Tasketier.App.Services;

/// <summary>
/// A named event between instances: a second <c>TasketierApp.exe --capture</c> sets it and exits, the running
/// instance listens and opens the capture box. One signal per action.
/// </summary>
public sealed class InstanceSignal : IDisposable
{
    private readonly EventWaitHandle _handle;
    private readonly CancellationTokenSource _stop = new();
    private Thread? _listener;

    /// <summary>Opens or creates the named event.</summary>
    /// <param name="name">The action name, for example <c>capture</c>.</param>
    public InstanceSignal(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        _handle = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\Tasketier-" + name);
    }

    /// <summary>Sets the event, for the instance that exits right after.</summary>
    public void Raise() => _handle.Set();

    /// <summary>Starts a background thread that runs an action each time the event is set.</summary>
    /// <param name="onSignal">The action; it runs on the listener thread, so marshal to the UI.</param>
    public void Listen(Action onSignal)
    {
        ArgumentNullException.ThrowIfNull(onSignal);
        if (_listener is not null)
        {
            throw new InvalidOperationException("The signal is already listening.");
        }
        _listener = new Thread(() =>
        {
            WaitHandle[] handles = [_handle, _stop.Token.WaitHandle];
            while (WaitHandle.WaitAny(handles) == 0)
            {
                onSignal();
            }
        })
        {
            IsBackground = true,
            Name = "Tasketier instance signal",
        };
        _listener.Start();
    }

    /// <summary>Stops listening and closes the event.</summary>
    public void Dispose()
    {
        _stop.Cancel();
        _listener?.Join(TimeSpan.FromSeconds(2));
        _stop.Dispose();
        _handle.Dispose();
    }
}
