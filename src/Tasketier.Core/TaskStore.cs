using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Tasketier.Core;

/// <summary>
/// The task file on disk: <c>tasks.json</c> in the data folder. Every read and every read-modify-write
/// runs under a cross-process mutex named after the file, so the app, the CLI and the MCP server never
/// race. Writes go to a temporary file first and replace the real one in one move. The first write of a
/// day copies the previous file into <c>backups/</c>.
/// </summary>
public sealed class TaskStore
{
    /// <summary>The file name inside the data folder.</summary>
    public const string FileName = "tasks.json";

    /// <summary>The backup folder inside the data folder.</summary>
    public const string BackupFolderName = "backups";

    private static readonly TimeSpan LockTimeout = TimeSpan.FromSeconds(10);

    private readonly TimeProvider _clock;
    private readonly string _mutexName;

    /// <summary>Creates the store for a data folder. The folder is created on the first write.</summary>
    /// <param name="folder">The data folder.</param>
    /// <param name="clock">The clock for backup dates.</param>
    public TaskStore(string folder, TimeProvider clock)
    {
        ArgumentNullException.ThrowIfNull(folder);
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
        Folder = Path.GetFullPath(folder);
        FilePath = Path.Combine(Folder, FileName);
        _mutexName = "Global\\Tasketier-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(FilePath.ToUpperInvariant())))[..32];
    }

    /// <summary>The data folder, as a full path.</summary>
    public string Folder { get; }

    /// <summary>The task file, as a full path.</summary>
    public string FilePath { get; }

    /// <summary>The backup folder, as a full path.</summary>
    public string BackupFolder => Path.Combine(Folder, BackupFolderName);

    /// <summary>Reads the document under the lock. A missing file is an empty document.</summary>
    /// <returns>The document.</returns>
    /// <exception cref="TaskStoreDamagedException">The file exists but is not valid.</exception>
    public TaskDocument Read()
    {
        using MutexLease lease = Acquire();
        return Load();
    }

    /// <summary>Reads the document, lets a function change it and writes it back, all under the lock.</summary>
    /// <typeparam name="T">What the function returns to the caller.</typeparam>
    /// <param name="change">The change; it may throw to leave the file untouched.</param>
    /// <returns>What the change returned.</returns>
    /// <exception cref="TaskStoreDamagedException">The file exists but is not valid.</exception>
    public T Update<T>(Func<TaskDocument, T> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        using MutexLease lease = Acquire();
        TaskDocument document = Load();
        T result = change(document);
        Save(document);
        return result;
    }

    /// <summary>Reads the document, lets an action change it and writes it back, all under the lock.</summary>
    /// <param name="change">The change; it may throw to leave the file untouched.</param>
    /// <exception cref="TaskStoreDamagedException">The file exists but is not valid.</exception>
    public void Update(Action<TaskDocument> change)
    {
        ArgumentNullException.ThrowIfNull(change);
        Update(document =>
        {
            change(document);
            return 0;
        });
    }

    private MutexLease Acquire()
    {
        var mutex = new Mutex(false, _mutexName);
        try
        {
            if (!mutex.WaitOne(LockTimeout))
            {
                throw new TimeoutException($"Another Tasketier process has held the lock on {FilePath} for more than {LockTimeout.TotalSeconds} s.");
            }
        }
        catch (AbandonedMutexException)
        {
            // The previous holder died mid-operation; its write was atomic or did not happen, so the file is whole.
        }
        catch
        {
            mutex.Dispose();
            throw;
        }
        return new MutexLease(mutex);
    }

    private TaskDocument Load()
    {
        if (!File.Exists(FilePath))
        {
            return new TaskDocument();
        }
        try
        {
            using FileStream stream = File.OpenRead(FilePath);
            TaskDocument document = JsonSerializer.Deserialize<TaskDocument>(stream, TaskJson.Options)
                ?? throw new JsonException("The file holds JSON null instead of a document.");
            if (document.Version > TaskDocument.CurrentVersion)
            {
                throw new JsonException(string.Format(CultureInfo.InvariantCulture, "The file is format version {0}; this Tasketier reads up to version {1}.", document.Version, TaskDocument.CurrentVersion));
            }
            return document;
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        {
            throw TaskStoreDamagedException.ForFile(FilePath, error);
        }
    }

    private void Save(TaskDocument document)
    {
        Directory.CreateDirectory(Folder);
        BackupOncePerDay();
        string temporary = FilePath + ".tmp";
        using (FileStream stream = File.Create(temporary))
        {
            JsonSerializer.Serialize(stream, document, TaskJson.Options);
        }
        File.Move(temporary, FilePath, overwrite: true);
    }

    private void BackupOncePerDay()
    {
        if (!File.Exists(FilePath))
        {
            return;
        }
        string today = _clock.GetLocalNow().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string backup = Path.Combine(BackupFolder, $"tasks-{today}.json");
        if (File.Exists(backup))
        {
            return;
        }
        Directory.CreateDirectory(BackupFolder);
        File.Copy(FilePath, backup);
    }

    /// <summary>Holds the mutex; dispose releases and closes it, so <c>using</c> covers both.</summary>
    private sealed class MutexLease : IDisposable
    {
        private readonly Mutex _mutex;

        public MutexLease(Mutex mutex)
        {
            _mutex = mutex;
        }

        public void Dispose()
        {
            _mutex.ReleaseMutex();
            _mutex.Dispose();
        }
    }
}
