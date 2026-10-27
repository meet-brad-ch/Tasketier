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
        Assert.Equal(3, settings.DefaultAlarmMinutes);
    }

    [Fact]
    public void Save_creates_the_folder_and_load_reads_it_back()
    {
        Store().Save(new AppSettings { CaptureHotkey = "Win+Shift+T", DataFolder = @"D:\sync", DefaultAlarmMinutes = 10 });

        AppSettings settings = Store().Load();

        Assert.Equal("Win+Shift+T", settings.CaptureHotkey);
        Assert.Equal(@"D:\sync", settings.DataFolder);
        Assert.Equal(10, settings.DefaultAlarmMinutes);
        Assert.False(File.Exists(Store().Path + ".tmp"));
    }

    [Fact]
    public void A_file_from_an_older_version_with_startWithWindows_still_loads()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Store().Path)!);
        File.WriteAllText(Store().Path, """{ "captureHotkey": "Ctrl+Alt+K", "startWithWindows": false }""");

        Assert.Equal("Ctrl+Alt+K", Store().Load().CaptureHotkey);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1441)]
    public void An_alarm_lead_outside_0_to_1440_is_an_error_that_names_the_file(int minutes)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Store().Path)!);
        File.WriteAllText(Store().Path, "{ \"defaultAlarmMinutes\": " + minutes + " }");

        SettingsDamagedException error = Assert.Throws<SettingsDamagedException>(() => Store().Load());

        Assert.Contains(Store().Path, error.Message, StringComparison.Ordinal);
        Assert.Contains("defaultAlarmMinutes", error.Message, StringComparison.Ordinal);
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
