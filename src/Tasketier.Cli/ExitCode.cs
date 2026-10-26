namespace Tasketier.Cli;

/// <summary>The process exit codes.</summary>
public static class ExitCode
{
    /// <summary>The command ran.</summary>
    public const int Ok = 0;

    /// <summary>The command was understood but could not be carried out: a parse error, an unknown id, a damaged file.</summary>
    public const int Failed = 1;

    /// <summary>The arguments were not understood.</summary>
    public const int Usage = 2;
}
