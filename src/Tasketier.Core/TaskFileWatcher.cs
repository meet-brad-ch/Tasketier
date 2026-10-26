namespace Tasketier.Core;

/// <summary>Raises <see cref="Changed"/> when the task file is written by any process, including this one.</summary>
public sealed class TaskFileWatcher : IDisposable
{
    private readonly FileSystemWatcher _watcher;

    /// <summary>Starts watching a store's file. The data folder is created when missing.</summary>
    /// <param name="store">The store.</param>
    public TaskFileWatcher(TaskStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        Directory.CreateDirectory(store.Folder);
        _watcher = new FileSystemWatcher(store.Folder, TaskStore.FileName)
        {
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
        };
        _watcher.Changed += OnChanged;
        _watcher.Created += OnChanged;
        _watcher.Renamed += OnChanged;
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>The file was written. Raised on a thread-pool thread, possibly several times per write.</summary>
    public event EventHandler? Changed;

    /// <summary>Stops watching.</summary>
    public void Dispose()
    {
        _watcher.EnableRaisingEvents = false;
        _watcher.Dispose();
    }

    private void OnChanged(object sender, FileSystemEventArgs e) => Changed?.Invoke(this, EventArgs.Empty);
}
