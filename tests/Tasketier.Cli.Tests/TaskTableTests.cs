using Tasketier.Core;

namespace Tasketier.Cli.Tests;

public sealed class TaskTableTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Render_aligns_ids_and_marks_overdue_done_priority_and_tags()
    {
        var table = new TaskTable(new TaskQueries(Now));
        TaskItem[] tasks =
        [
            new() { Id = 3, Title = "Overdue", Due = new DateOnly(2026, 10, 25) },
            new() { Id = 12, Title = "Timed", Due = new DateOnly(2026, 10, 26), DueTime = new TimeOnly(15, 30), Priority = Priority.Urgent, Tags = ["home", "shop"] },
            new() { Id = 5, Title = "Planned", PlannedFor = new DateOnly(2026, 10, 26) },
            new() { Id = 7, Title = "Done", Completed = Now },
        ];

        string[] lines = table.Render(tasks).Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);

        Assert.StartsWith(" 3  [ ]  Sun 2026-10-25 !", lines[0], StringComparison.Ordinal);
        Assert.EndsWith("Overdue", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("12  [ ]  Mon 2026-10-26 15:30", lines[1], StringComparison.Ordinal);
        Assert.Contains("!!  Timed  #home  #shop", lines[1], StringComparison.Ordinal);
        Assert.StartsWith(" 5  [ ]  planned", lines[2], StringComparison.Ordinal);
        Assert.StartsWith(" 7  [x]  ", lines[3], StringComparison.Ordinal);
        int[] titleColumns = [lines[0].IndexOf("Overdue", StringComparison.Ordinal), lines[1].IndexOf("Timed", StringComparison.Ordinal), lines[2].IndexOf("Planned", StringComparison.Ordinal), lines[3].IndexOf("Done", StringComparison.Ordinal)];
        Assert.All(titleColumns, column => Assert.Equal(titleColumns[0], column));
    }

    [Fact]
    public void Render_of_nothing_says_so()
    {
        Assert.Equal("(no tasks)" + Environment.NewLine, new TaskTable(new TaskQueries(Now)).Render([]));
    }

    [Fact]
    public void OneLine_is_the_id_and_the_title()
    {
        Assert.Equal("#4 Milk", TaskTable.OneLine(new TaskItem { Id = 4, Title = "Milk" }));
    }
}
