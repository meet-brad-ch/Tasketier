using System.Globalization;
using System.Text.Json;
using Tasketier.Core;

namespace Tasketier.Cli;

/// <summary>Runs one command line against the store and writes the result.</summary>
public sealed class CommandRunner
{
    private const string Help = """
        tasketier - local task capture and planning

        tasketier add <text>                 add a task; text uses the inline syntax (see below)
        tasketier list [--view today|upcoming|inbox|done] [--filter <text>]
        tasketier show <id>
        tasketier done <id>                  complete
        tasketier undone <id>                reopen
        tasketier edit <id> <text>           replace title, tags, priority and due from the text
        tasketier delete <id>
        tasketier plan <id>                  pull into Today without a due date
        tasketier unplan <id>
        tasketier search <text>
        tasketier mcp                        run the MCP server on stdin/stdout
        tasketier version

        Options: --json (records instead of lines), --data <folder> (the folder with tasks.json).

        Inline syntax: words are the title; #tag; ! high, !! urgent; @today @tomorrow @fri
        @2026-11-03 @+3d set the due day; @15:30 the due time (today when no day); @none clears.
        """;

    private readonly TextWriter _out;
    private readonly TextWriter _error;
    private readonly TimeProvider _clock;

    /// <summary>Creates the runner.</summary>
    /// <param name="output">Where results go.</param>
    /// <param name="error">Where errors go.</param>
    /// <param name="clock">The clock.</param>
    public CommandRunner(TextWriter output, TextWriter error, TimeProvider clock)
    {
        _out = output ?? throw new ArgumentNullException(nameof(output));
        _error = error ?? throw new ArgumentNullException(nameof(error));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    /// <summary>Runs the arguments.</summary>
    /// <param name="args">The command line.</param>
    /// <returns>The exit code.</returns>
    public async Task<int> RunAsync(IReadOnlyList<string> args)
    {
        try
        {
            var arguments = new Arguments(args);
            if (arguments.Command == "mcp")
            {
                await McpServerHost.RunAsync(ServiceFor(arguments)).ConfigureAwait(false);
                return ExitCode.Ok;
            }
            Run(arguments);
            return ExitCode.Ok;
        }
        catch (UsageException error)
        {
            await _error.WriteLineAsync(error.Message).ConfigureAwait(false);
            await _error.WriteLineAsync("Run 'tasketier help' for the commands.").ConfigureAwait(false);
            return ExitCode.Usage;
        }
        catch (Exception error) when (error is TaskParseException or TaskNotFoundException or TaskStoreDamagedException or SettingsDamagedException or TimeoutException)
        {
            await _error.WriteLineAsync(error.Message).ConfigureAwait(false);
            return ExitCode.Failed;
        }
    }

    private void Run(Arguments arguments)
    {
        switch (arguments.Command)
        {
            case "" or "help" or "--help" or "-h":
                _out.WriteLine(Help);
                return;
            case "version":
                _out.WriteLine(typeof(CommandRunner).Assembly.GetName().Version?.ToString(3) ?? "unknown");
                return;
        }
        TaskService service = ServiceFor(arguments);
        bool json = arguments.Has("json");
        switch (arguments.Command)
        {
            case "add":
                Print(service.Add(Text(arguments, "add")), json, "Added ");
                break;
            case "list":
                Print(service.List(View(arguments.Value("view")), arguments.Value("filter") ?? string.Empty), service, json);
                break;
            case "show":
                Print(service.Get(Id(arguments)), json, string.Empty);
                break;
            case "done":
                Print(service.Complete(Id(arguments)), json, "Completed ");
                break;
            case "undone":
                Print(service.Uncomplete(Id(arguments)), json, "Reopened ");
                break;
            case "edit":
                Print(service.Edit(Id(arguments), string.Join(' ', arguments.Rest.Skip(1))), json, "Edited ");
                break;
            case "delete":
                Print(service.Delete(Id(arguments)), json, "Deleted ");
                break;
            case "plan":
                Print(service.Plan(Id(arguments)), json, "Planned for today: ");
                break;
            case "unplan":
                Print(service.Unplan(Id(arguments)), json, "Unplanned ");
                break;
            case "search":
                Print(service.Search(Text(arguments, "search")), service, json);
                break;
            default:
                throw new UsageException($"Unknown command '{arguments.Command}'.");
        }
    }

    private TaskService ServiceFor(Arguments arguments)
    {
        string folder = DataLocation.Resolve(arguments.Value("data"));
        return new TaskService(new TaskStore(folder, _clock), _clock);
    }

    private static string Text(Arguments arguments, string command)
    {
        string text = arguments.RestText;
        if (text.Length == 0)
        {
            throw new UsageException($"'{command}' needs the task text: tasketier {command} Buy milk @tomorrow");
        }
        return text;
    }

    private static int Id(Arguments arguments)
    {
        if (arguments.Rest.Count == 0 || !int.TryParse(arguments.Rest[0], NumberStyles.None, CultureInfo.InvariantCulture, out int id))
        {
            throw new UsageException($"'{arguments.Command}' needs a task id: tasketier {arguments.Command} 12");
        }
        return id;
    }

    private static TaskView View(string? name) => (name ?? "today").ToUpperInvariant() switch
    {
        "TODAY" => TaskView.Today,
        "UPCOMING" => TaskView.Upcoming,
        "INBOX" => TaskView.Inbox,
        "DONE" => TaskView.Done,
        _ => throw new UsageException($"Unknown view '{name}'; use today, upcoming, inbox or done."),
    };

    private void Print(TaskItem task, bool json, string prefix)
    {
        if (json)
        {
            _out.WriteLine(JsonSerializer.Serialize(task, TaskJson.Options));
        }
        else
        {
            _out.WriteLine(prefix + TaskTable.OneLine(task));
        }
    }

    private void Print(IReadOnlyList<TaskItem> tasks, TaskService service, bool json)
    {
        if (json)
        {
            _out.WriteLine(JsonSerializer.Serialize(tasks, TaskJson.Options));
        }
        else
        {
            _out.Write(new TaskTable(service.Queries()).Render(tasks));
        }
    }
}
