namespace Tasketier.Core.Tests;

public sealed class TaskStoreTests : IDisposable
{
    private readonly TempFolder _folder = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 10, 26, 12, 0, 0, TimeSpan.Zero));

    public void Dispose() => _folder.Dispose();

    private TaskStore Store() => new(_folder.Path, _clock);

    [Fact]
    public void A_missing_file_reads_as_an_empty_document()
    {
        TaskDocument document = Store().Read();

        Assert.Empty(document.Tasks);
        Assert.Equal(1, document.NextId);
        Assert.False(File.Exists(Store().FilePath));
    }

    [Fact]
    public void Update_writes_the_file_and_a_second_store_reads_it_back()
    {
        Store().Update(document =>
        {
            document.Tasks.Add(new TaskItem { Id = 7, Title = "Milk", Due = new DateOnly(2026, 10, 30), DueTime = new TimeOnly(9, 15), Tags = ["home"], Priority = Priority.High, Created = _clock.Now });
            document.NextId = 8;
            return 0;
        });

        TaskDocument read = Store().Read();

        TaskItem task = Assert.Single(read.Tasks);
        Assert.Equal(8, read.NextId);
        Assert.Equal(7, task.Id);
        Assert.Equal("Milk", task.Title);
        Assert.Equal(new DateOnly(2026, 10, 30), task.Due);
        Assert.Equal(new TimeOnly(9, 15), task.DueTime);
        Assert.Equal(["home"], task.Tags);
        Assert.Equal(Priority.High, task.Priority);
        Assert.False(File.Exists(Store().FilePath + ".tmp"));
    }

    [Fact]
    public void The_file_is_readable_json_with_enum_names()
    {
        Store().Update(document =>
        {
            document.Tasks.Add(new TaskItem { Id = 1, Title = "Milk", Priority = Priority.Urgent });
            return 0;
        });

        string text = File.ReadAllText(Store().FilePath);

        Assert.Contains("\"priority\": \"Urgent\"", text, StringComparison.Ordinal);
        Assert.Contains("\"title\": \"Milk\"", text, StringComparison.Ordinal);
        Assert.DoesNotContain("\"due\"", text, StringComparison.Ordinal);
    }

    [Fact]
    public void A_change_that_throws_leaves_the_file_untouched()
    {
        Store().Update(document =>
        {
            document.Tasks.Add(new TaskItem { Id = 1, Title = "Keep" });
            return 0;
        });

        Assert.Throws<InvalidOperationException>(() => Store().Update<int>(document =>
        {
            document.Tasks.Clear();
            throw new InvalidOperationException("no");
        }));

        Assert.Single(Store().Read().Tasks);
    }

    [Fact]
    public void A_damaged_file_is_reported_with_its_path_and_never_replaced()
    {
        File.WriteAllText(Store().FilePath, "{ not json");

        TaskStoreDamagedException error = Assert.Throws<TaskStoreDamagedException>(() => Store().Read());

        Assert.Contains(Store().FilePath, error.Message, StringComparison.Ordinal);
        Assert.Contains("backups", error.Message, StringComparison.Ordinal);
        Assert.Equal("{ not json", File.ReadAllText(Store().FilePath));
    }

    [Fact]
    public void A_newer_format_version_is_reported_not_rewritten()
    {
        File.WriteAllText(Store().FilePath, "{ \"version\": 99, \"nextId\": 1, \"tasks\": [] }");

        Assert.Throws<TaskStoreDamagedException>(() => Store().Update(_ => 0));

        Assert.Contains("99", File.ReadAllText(Store().FilePath), StringComparison.Ordinal);
    }

    [Fact]
    public void The_first_write_of_a_day_copies_the_previous_file_into_backups()
    {
        TaskStore store = Store();
        store.Update(d => d.Tasks.Add(new TaskItem { Id = 1, Title = "First" }));
        Assert.False(Directory.Exists(store.BackupFolder));  // nothing to back up before the first file

        store.Update(d => d.Tasks.Add(new TaskItem { Id = 2, Title = "Second" }));
        store.Update(d => d.Tasks.Add(new TaskItem { Id = 3, Title = "Third" }));

        string backup = Path.Combine(store.BackupFolder, "tasks-2026-10-26.json");
        Assert.True(File.Exists(backup));
        Assert.Contains("First", File.ReadAllText(backup), StringComparison.Ordinal);
        Assert.DoesNotContain("Second", File.ReadAllText(backup), StringComparison.Ordinal);

        _clock.Advance(TimeSpan.FromDays(1));
        store.Update(d => d.Tasks.Add(new TaskItem { Id = 4, Title = "Fourth" }));

        Assert.Contains("Third", File.ReadAllText(Path.Combine(store.BackupFolder, "tasks-2026-10-27.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void Concurrent_updates_from_several_stores_lose_nothing()
    {
        const int writers = 8;
        const int perWriter = 25;

        Parallel.For(0, writers, writer =>
        {
            TaskStore store = Store();
            for (int i = 0; i < perWriter; i++)
            {
                store.Update(document =>
                {
                    document.Tasks.Add(new TaskItem { Id = document.NextId, Title = $"w{writer} #{i}" });
                    document.NextId++;
                    return 0;
                });
            }
        });

        TaskDocument document = Store().Read();
        Assert.Equal(writers * perWriter, document.Tasks.Count);
        Assert.Equal(writers * perWriter + 1, document.NextId);
        Assert.Equal(document.Tasks.Count, document.Tasks.Select(t => t.Id).Distinct().Count());
    }

    [Fact]
    public void The_watcher_reports_a_write_by_another_store()
    {
        TaskStore store = Store();
        using var watcher = new TaskFileWatcher(store);
        using var seen = new ManualResetEventSlim();
        watcher.Changed += (_, _) => seen.Set();

        Store().Update(d => d.Tasks.Add(new TaskItem { Id = 1, Title = "From elsewhere" }));

        Assert.True(seen.Wait(TimeSpan.FromSeconds(5)));
    }
}
