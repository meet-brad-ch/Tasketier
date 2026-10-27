using System.Text.Json;

namespace Tasketier.Core;

/// <summary>The settings file on disk. A missing file means the defaults; a damaged one is an error.</summary>
public sealed class SettingsStore
{
    private readonly string _path;

    /// <summary>Creates the store for a file.</summary>
    /// <param name="path">The JSON file, for example <c>%LOCALAPPDATA%\Tasketier\settings.json</c>.</param>
    public SettingsStore(string path)
    {
        _path = path ?? throw new ArgumentNullException(nameof(path));
    }

    /// <summary>The file.</summary>
    public string Path => _path;

    /// <summary>Reads the settings.</summary>
    /// <returns>The settings, or the defaults when the file does not exist.</returns>
    /// <exception cref="SettingsDamagedException">The file exists but cannot be read.</exception>
    public AppSettings Load()
    {
        if (!File.Exists(_path))
        {
            return new AppSettings();
        }
        try
        {
            AppSettings settings;
            using (FileStream stream = File.OpenRead(_path))
            {
                settings = JsonSerializer.Deserialize<AppSettings>(stream, TaskJson.Options)
                    ?? throw new JsonException("The file holds JSON null instead of settings.");
            }
            if (settings.DefaultAlarmMinutes is < 0 or > TaskParser.MaxAlarmMinutes)
            {
                throw new SettingsDamagedException($"The settings file {_path} sets defaultAlarmMinutes to {settings.DefaultAlarmMinutes}. It must be 0 to {TaskParser.MaxAlarmMinutes}. Fix or delete it.");
            }
            return settings;
        }
        catch (Exception error) when (error is JsonException or IOException or UnauthorizedAccessException)
        {
            throw new SettingsDamagedException($"The settings file {_path} cannot be read: {error.Message} Fix or delete it.", error);
        }
    }

    /// <summary>Writes the settings.</summary>
    /// <param name="settings">The settings.</param>
    public void Save(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(_path)!);
        string temporary = _path + ".tmp";
        using (FileStream stream = File.Create(temporary))
        {
            JsonSerializer.Serialize(stream, settings, TaskJson.Options);
        }
        File.Move(temporary, _path, overwrite: true);
    }
}
