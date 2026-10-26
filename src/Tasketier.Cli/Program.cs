namespace Tasketier.Cli;

/// <summary>The entry point of <c>tasketier.exe</c>.</summary>
public static class Program
{
    /// <summary>Runs the command line.</summary>
    /// <param name="args">The arguments.</param>
    /// <returns>0 on success, 1 when the request could not be carried out, 2 on a usage error.</returns>
    public static async Task<int> Main(string[] args)
    {
        var runner = new CommandRunner(Console.Out, Console.Error, TimeProvider.System);
        return await runner.RunAsync(args).ConfigureAwait(false);
    }
}
