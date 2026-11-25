using System.IO;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Tests;

public sealed class LauncherSettingsStoreShould : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"holonet-settings-{Guid.NewGuid():N}");

    private string FilePath => Path.Combine(_directory, "nested", "settings.json");

    [Fact]
    public async Task KeepASavedChoice_AcrossRestarts()
    {
        await CreateStore().SaveAsync(new LauncherSettings(ScreensaverStyle.BouncingLogo));

        var restarted = CreateStore();
        await restarted.LoadAsync();

        Assert.Equal(ScreensaverStyle.BouncingLogo, restarted.Current.Screensaver);
        Assert.Contains("\"BouncingLogo\"", await File.ReadAllTextAsync(FilePath));
    }

    [Fact]
    public async Task FallBackToTheDefaults_WhenTheFileIsDamaged()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        await File.WriteAllTextAsync(FilePath, "{ not json");
        var store = CreateStore();

        await store.LoadAsync();

        Assert.Equal(new LauncherSettings(), store.Current);
    }

    private LauncherSettingsStore CreateStore() => new(
        Options.Create(new TvLauncherOptions { SettingsFilePath = FilePath }),
        NullLogger<LauncherSettingsStore>.Instance);

    public void Dispose()
    {
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }
}
