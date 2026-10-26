using Tasketier.Core;

namespace Tasketier.Cli;

/// <summary>Where the CLI and the MCP server find the task file: <c>--data</c>, else the settings' data folder, else the default folder.</summary>
public static class DataLocation
{
    /// <summary>Resolves the data folder and the settings that shape parsing.</summary>
    /// <param name="dataOption">The <c>--data</c> value, or null. With a value, the settings file is not read.</param>
    /// <returns>The folder and the default alarm minutes.</returns>
    /// <exception cref="SettingsDamagedException">The settings file exists but cannot be read.</exception>
    public static DataSource Resolve(string? dataOption)
    {
        if (!string.IsNullOrWhiteSpace(dataOption))
        {
            return new DataSource(Path.GetFullPath(dataOption), AppSettings.DefaultAlarmMinutesValue);
        }
        var paths = new AppPaths();
        AppSettings settings = new SettingsStore(paths.SettingsFile).Load();
        return new DataSource(paths.DataFolder(settings), settings.DefaultAlarmMinutes);
    }
}

/// <summary>The data folder and the settings a service needs.</summary>
/// <param name="Folder">The folder with <c>tasks.json</c>.</param>
/// <param name="DefaultAlarmMinutes">The minutes a bare <c>@alarm</c> means.</param>
public sealed record DataSource(string Folder, int DefaultAlarmMinutes);
