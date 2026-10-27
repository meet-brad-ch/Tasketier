using System.Text.Json;
using Tasketier.Core;

namespace Tasketier.Cli.Tests;

public sealed class CommandRunnerTests : IDisposable
{
    private readonly TempFolder _folder = new();
    private readonly StringWriter _out = new();
    private readonly StringWriter _error = new();
    private readonly CommandRunner _runner;

    public CommandRunnerTests()
    {
        _runner = new CommandRunner(_out, _error, TimeProvider.System);
    }

    public void Dispose()
    {
        _folder.Dispose();
        _out.Dispose();
        _error.Dispose();
    }

    private async Task<int> Run(params string[] args) => await _runner.RunAsync([.. args, "--data", _folder.Path]);

    [Fact]
    public async Task Add_then_list_prints_the_task()
    {
        Assert.Equal(ExitCode.Ok, await Run("add", "Buy", "milk", "@today", "#home"));
        Assert.Equal(ExitCode.Ok, await Run("list"));

        string output = _out.ToString();
        Assert.Contains("Added #1 Buy milk", output, StringComparison.Ordinal);
        Assert.Contains("[ ]", output, StringComparison.Ordinal);
        Assert.Contains("Buy milk  #home", output, StringComparison.Ordinal);
        Assert.Equal(string.Empty, _error.ToString());
    }

    [Fact]
    public async Task Json_output_is_the_record()
    {
        await Run("add", "Milk", "!!", "--json");

        TaskItem? task = JsonSerializer.Deserialize<TaskItem>(_out.ToString(), TaskJson.Options);

        Assert.NotNull(task);
        Assert.Equal(1, task.Id);
        Assert.Equal(Priority.Urgent, task.Priority);
    }

    [Fact]
    public async Task List_json_is_an_array_of_the_view()
    {
        await Run("add", "Later", "@+3d");
        await Run("add", "Now", "@today");
        _out.GetStringBuilder().Clear();

        await Run("list", "--view", "upcoming", "--json");

        TaskItem[]? tasks = JsonSerializer.Deserialize<TaskItem[]>(_out.ToString(), TaskJson.Options);
        Assert.NotNull(tasks);
        Assert.Equal(["Later"], tasks.Select(t => t.Title));
    }

    [Fact]
    public async Task List_all_shows_every_task_open_ones_first()
    {
        await Run("add", "Later", "@+3d");
        await Run("add", "Now", "@today");
        await Run("add", "Someday");
        await Run("done", "3");
        _out.GetStringBuilder().Clear();

        await Run("list", "--view", "all", "--json");

        TaskItem[]? tasks = JsonSerializer.Deserialize<TaskItem[]>(_out.ToString(), TaskJson.Options);
        Assert.NotNull(tasks);
        Assert.Equal(["Now", "Later", "Someday"], tasks.Select(t => t.Title));
    }

    [Fact]
    public async Task Done_undone_plan_unplan_edit_delete_round_trip()
    {
        await Run("add", "Milk");

        Assert.Equal(ExitCode.Ok, await Run("done", "1"));
        Assert.Contains("Completed #1 Milk", _out.ToString(), StringComparison.Ordinal);
        Assert.Equal(ExitCode.Ok, await Run("undone", "1"));
        Assert.Equal(ExitCode.Ok, await Run("plan", "1"));
        Assert.Equal(ExitCode.Ok, await Run("unplan", "1"));
        Assert.Equal(ExitCode.Ok, await Run("edit", "1", "Oat", "milk", "#shop"));
        Assert.Contains("Edited #1 Oat milk", _out.ToString(), StringComparison.Ordinal);
        Assert.Equal(ExitCode.Ok, await Run("show", "1"));
        Assert.Equal(ExitCode.Ok, await Run("search", "oat"));
        Assert.Contains("Oat milk  #shop", _out.ToString(), StringComparison.Ordinal);
        Assert.Equal(ExitCode.Ok, await Run("delete", "1"));
        Assert.Contains("Deleted #1 Oat milk", _out.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task An_unknown_id_fails_with_the_message_on_stderr()
    {
        Assert.Equal(ExitCode.Failed, await Run("done", "9"));

        Assert.Contains("no task with id 9", _error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_bad_date_mark_fails_without_storing()
    {
        Assert.Equal(ExitCode.Failed, await Run("add", "Milk", "@soon"));

        Assert.Contains("@soon", _error.ToString(), StringComparison.Ordinal);
        _out.GetStringBuilder().Clear();
        await Run("list", "--view", "inbox");
        Assert.Contains("(no tasks)", _out.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("frobnicate")]
    [InlineData("done")]
    [InlineData("done", "x")]
    [InlineData("add")]
    [InlineData("list", "--view", "someday")]
    public async Task Usage_errors_exit_with_2_and_a_hint(params string[] args)
    {
        Assert.Equal(ExitCode.Usage, await Run(args));

        Assert.Contains("tasketier", _error.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Help_and_version_print_without_touching_the_store()
    {
        Assert.Equal(ExitCode.Ok, await _runner.RunAsync(["help"]));
        Assert.Contains("Inline syntax", _out.ToString(), StringComparison.Ordinal);
        Assert.Equal(ExitCode.Ok, await _runner.RunAsync(["version"]));
        Assert.Contains("0.2.7", _out.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task A_damaged_task_file_is_reported_and_kept()
    {
        string file = Path.Combine(_folder.Path, TaskStore.FileName);
        await File.WriteAllTextAsync(file, "{ broken");

        Assert.Equal(ExitCode.Failed, await Run("list"));

        Assert.Contains(file, _error.ToString(), StringComparison.Ordinal);
        Assert.Equal("{ broken", await File.ReadAllTextAsync(file));
    }
}
