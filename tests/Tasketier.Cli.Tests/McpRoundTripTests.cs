using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using Tasketier.Core;

namespace Tasketier.Cli.Tests;

/// <summary>Starts the real exe as an MCP server over stdio and drives it with the SDK client.</summary>
public sealed class McpRoundTripTests : IAsyncLifetime, IDisposable
{
    private readonly TempFolder _folder = new();
    private McpClient? _client;

    public async Task InitializeAsync()
    {
        string dll = Path.Combine(AppContext.BaseDirectory, "tasketier.dll");
        Assert.True(File.Exists(dll), $"The CLI build output is missing at {dll}.");
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "tasketier",
            Command = "dotnet",
            Arguments = [dll, "mcp", "--data", _folder.Path],
        });
        _client = await McpClient.CreateAsync(transport);
    }

    public async Task DisposeAsync()
    {
        if (_client is not null)
        {
            await _client.DisposeAsync();
        }
        Dispose();
    }

    public void Dispose() => _folder.Dispose();

    [Fact]
    public async Task The_server_lists_every_tool()
    {
        IList<McpClientTool> tools = await _client!.ListToolsAsync();

        string[] names = [.. tools.Select(t => t.Name).Order()];
        Assert.Equal(["add_task", "complete_task", "delete_task", "get_task", "list_tasks", "plan_today", "reopen_task", "search_tasks", "update_task"], names);
        Assert.Contains("@tomorrow", tools.Single(t => t.Name == "add_task").Description, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Add_list_complete_and_delete_round_trip_through_the_tools()
    {
        TaskItem added = await Call<TaskItem>("add_task", new { text = "Call Bob @today #work !" });
        Assert.Equal("Call Bob", added.Title);
        Assert.Equal(["work"], added.Tags);
        Assert.Equal(Priority.High, added.Priority);

        TaskItem[] today = await Call<TaskItem[]>("list_tasks", new { view = "today" });
        Assert.Equal([added.Id], today.Select(t => t.Id));

        TaskItem done = await Call<TaskItem>("complete_task", new { id = added.Id });
        Assert.True(done.IsDone);
        Assert.Empty(await Call<TaskItem[]>("list_tasks", new { view = "today" }));
        Assert.Single(await Call<TaskItem[]>("list_tasks", new { view = "done" }));

        TaskItem reopened = await Call<TaskItem>("reopen_task", new { id = added.Id });
        Assert.False(reopened.IsDone);

        TaskItem planned = await Call<TaskItem>("plan_today", new { id = added.Id, planned = true });
        Assert.NotNull(planned.PlannedFor);

        TaskItem updated = await Call<TaskItem>("update_task", new { id = added.Id, text = "Call Alice" });
        Assert.Equal("Call Alice", updated.Title);
        Assert.Empty(updated.Tags);

        TaskItem[] found = await Call<TaskItem[]>("search_tasks", new { text = "alice" });
        Assert.Single(found);

        TaskItem got = await Call<TaskItem>("get_task", new { id = added.Id });
        Assert.Equal("Call Alice", got.Title);

        TaskItem deleted = await Call<TaskItem>("delete_task", new { id = added.Id });
        Assert.Equal(added.Id, deleted.Id);
    }

    [Fact]
    public async Task A_tool_error_comes_back_as_an_error_result_with_the_message()
    {
        CallToolResult result = await _client!.CallToolAsync("complete_task", new Dictionary<string, object?> { ["id"] = 404 });

        Assert.True(result.IsError);
        Assert.Contains("404", string.Join(' ', result.Content.OfType<TextContentBlock>().Select(c => c.Text)), StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_file_the_server_writes_is_the_store_s_file()
    {
        await Call<TaskItem>("add_task", new { text = "On disk" });

        var store = new TaskStore(_folder.Path, TimeProvider.System);
        Assert.Equal(["On disk"], store.Read().Tasks.Select(t => t.Title));
    }

    private async Task<T> Call<T>(string tool, object arguments)
    {
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, object?>>(JsonSerializer.Serialize(arguments))!;
        CallToolResult result = await _client!.CallToolAsync(tool, dictionary);
        string text = string.Join(' ', result.Content.OfType<TextContentBlock>().Select(c => c.Text));
        Assert.False(result.IsError ?? false, JsonSerializer.Serialize(result));
        return JsonSerializer.Deserialize<T>(text, TaskJson.Options)!;
    }
}
