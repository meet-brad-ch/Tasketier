namespace Tasketier.Cli;

/// <summary>The command line taken apart: the command, its positional words and the <c>--options</c>.</summary>
public sealed class Arguments
{
    private readonly Dictionary<string, string> _options = new(StringComparer.Ordinal);
    private readonly List<string> _positional = [];

    /// <summary>Takes a command line apart. The first word is the command; <c>--name value</c> and <c>--flag</c> are options.</summary>
    /// <param name="args">The arguments as the shell passed them.</param>
    /// <exception cref="UsageException">An option is written twice.</exception>
    public Arguments(IReadOnlyList<string> args)
    {
        ArgumentNullException.ThrowIfNull(args);
        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            if (arg.StartsWith("--", StringComparison.Ordinal) && arg.Length > 2)
            {
                string name = arg[2..];
                bool hasValue = i + 1 < args.Count && !args[i + 1].StartsWith("--", StringComparison.Ordinal);
                string value = hasValue ? args[++i] : string.Empty;
                if (!_options.TryAdd(name, value))
                {
                    throw new UsageException($"The option --{name} is given twice.");
                }
            }
            else
            {
                _positional.Add(arg);
            }
        }
    }

    /// <summary>The first positional word, or empty.</summary>
    public string Command => _positional.Count > 0 ? _positional[0] : string.Empty;

    /// <summary>The positional words after the command.</summary>
    public IReadOnlyList<string> Rest => _positional.Count > 1 ? _positional[1..] : [];

    /// <summary>The positional words after the command joined by spaces: the task text.</summary>
    public string RestText => string.Join(' ', Rest);

    /// <summary>Whether an option was given, with or without a value.</summary>
    /// <param name="name">The name without the dashes.</param>
    /// <returns>True when present.</returns>
    public bool Has(string name) => _options.ContainsKey(name);

    /// <summary>An option's value.</summary>
    /// <param name="name">The name without the dashes.</param>
    /// <returns>The value, or null when the option is absent.</returns>
    public string? Value(string name) => _options.TryGetValue(name, out string? value) ? value : null;

    /// <summary>The names of every option that was given.</summary>
    public IReadOnlyCollection<string> OptionNames => _options.Keys;
}
