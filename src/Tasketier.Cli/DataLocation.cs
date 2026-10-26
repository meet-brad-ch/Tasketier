using Tasketier.Core;

namespace Tasketier.Cli;

/// <summary>Where the CLI and the MCP server find the task file: <c>--data</c>, else the settings' data folder, else the default folder.</summary>
public static class DataLocation
{
    /// <summary>Resolves the data folder.</summary>
    /// <param name="dataOption">The <c>--data</c> value, or null.</param>
    /// <returns>The folder.</returns>
    /// <exception cref="SettingsDamagedException">The settings file exists but cannot be read.</exception>
    public static string Resolve(string? dataOption)
    {
        if (!string.IsNullOrWhiteSpace(dataOption))
        {
            return Path.GetFullPath(dataOption);
        }
        var paths = new AppPaths();
        AppSettings settings = new SettingsStore(paths.SettingsFile).Load();
        return paths.DataFolder(settings);
    }
}
