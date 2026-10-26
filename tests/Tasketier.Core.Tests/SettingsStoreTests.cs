namespace Tasketier.Core.Tests;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly TempFolder _folder = new();

    public void Dispose() => _folder.Dispose();

    private SettingsStore Store() => new(Path.Combine(_folder.Path, "sub", "settings.json"));

    [Fact]
    public void A_missing_file_gives_the_defaults()
    {
        AppSettings settings = Store().Load();

        Assert.Equal("Ctrl+Alt+A", settings.CaptureHotkey);
        Assert.Equal("Ctrl+Alt+L", settings.ListHotkey);
        Assert.Equal(string.Empty, settings.DataFolder);
        Assert.False(settings.StartWithWindows);
    }

    [Fact]
    public void Save_creates_the_folder_and_load_reads_it_back()
    {
        Store().Save(new AppSettings { CaptureHotkey = "Win+Shift+T", DataFolder = @"D:\sync", StartWithWindows = true });

        AppSettings settings = Store().Load();

        Assert.Equal("Win+Shift+T", settings.CaptureHotkey);
        Assert.Equal(@"D:\sync", settings.DataFolder);
        Assert.True(settings.StartWithWindows);
    }

    [Fact]
    public void A_damaged_file_is_an_error_that_names_the_file()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Store().Path)!);
        File.WriteAllText(Store().Path, "nope");

        SettingsDamagedException error = Assert.Throws<SettingsDamagedException>(() => Store().Load());

        Assert.Contains("settings.json", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_data_folder_falls_back_to_the_default_folder()
    {
        var paths = new AppPaths(_folder.Path);

        Assert.Equal(_folder.Path, paths.DataFolder(new AppSettings()));
        Assert.Equal(@"D:\sync", paths.DataFolder(new AppSettings { DataFolder = @"D:\sync" }));
        Assert.Equal(Path.Combine(_folder.Path, "settings.json"), paths.SettingsFile);
    }
}
