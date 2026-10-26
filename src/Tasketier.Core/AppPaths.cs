namespace Tasketier.Core;

/// <summary>Where Tasketier keeps its per-user files: <c>%LOCALAPPDATA%\Tasketier</c>, unless the settings move the data.</summary>
public sealed class AppPaths
{
    /// <summary>Uses the current user's local application data folder.</summary>
    public AppPaths()
        : this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Tasketier"))
    {
    }

    /// <summary>Uses a given folder as the default folder.</summary>
    /// <param name="folder">The folder.</param>
    public AppPaths(string folder)
    {
        DefaultFolder = folder ?? throw new ArgumentNullException(nameof(folder));
    }

    /// <summary>The folder with the settings and, by default, the tasks.</summary>
    public string DefaultFolder { get; }

    /// <summary>The settings file; always in the default folder.</summary>
    public string SettingsFile => Path.Combine(DefaultFolder, "settings.json");

    /// <summary>The folder that holds <c>tasks.json</c> for given settings.</summary>
    /// <param name="settings">The settings.</param>
    /// <returns>The settings' data folder, or the default folder when it is empty.</returns>
    public string DataFolder(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return string.IsNullOrWhiteSpace(settings.DataFolder) ? DefaultFolder : settings.DataFolder;
    }
}
