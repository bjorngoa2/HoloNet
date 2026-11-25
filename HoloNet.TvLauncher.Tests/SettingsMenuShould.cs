using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

public class SettingsMenuShould
{
    [Fact]
    public async Task SaveAndPreviewAChosenScreensaver_AndMoveTheCheckMarkToIt()
    {
        var store = new FakeLauncherSettingsStore(new LauncherSettings(ScreensaverStyle.AmbientClock));
        var previews = 0;
        var screensaverFolder = OpenScreensaverFolder(Menu(store, () => previews++));
        var options = screensaverFolder.GetChildren().Cast<SettingChoiceCardViewModel>().ToList();
        var ambient = options.Single(o => o.Title == "Ambient clock");
        var bouncing = options.Single(o => o.Title == "Bouncing logo");
        Assert.Equal("Ambient clock", screensaverFolder.Subtitle);
        Assert.Equal(TileIcons.Check, ambient.IconGlyph);

        await bouncing.ChooseAsync();

        Assert.Equal(new LauncherSettings(ScreensaverStyle.BouncingLogo), Assert.Single(store.Saved));
        Assert.Equal(1, previews);
        Assert.Equal(TileIcons.Check, bouncing.IconGlyph);
        Assert.NotEqual(TileIcons.Check, ambient.IconGlyph);
        Assert.Equal("Screensaver · In use", bouncing.Subtitle);
        Assert.Equal("Bouncing logo", screensaverFolder.Subtitle);
    }

    [Fact]
    public void OfferAnUpdateCheck_ShowingTheRunningVersion_WithoutCountingItAsASetting()
    {
        var settings = Menu(new FakeLauncherSettingsStore()).CreateFolder();

        var checkForUpdates = Assert.IsType<CheckForUpdatesActionCardViewModel>(Assert.Single(settings.GetChildren(), card => card.Title == "Check for updates"));
        Assert.Equal("Version 0.6.0", checkForUpdates.Subtitle);
        Assert.Contains("HoloNet 0.6.0", checkForUpdates.Description);
        Assert.Equal("1 setting", settings.Subtitle);
    }

    private static SettingsMenu Menu(FakeLauncherSettingsStore store, Action? previewScreensaver = null) =>
        new(new TvLauncherOptions(), store, previewScreensaver ?? (() => { }), currentVersion: "0.6.0");

    private static FolderCardViewModel OpenScreensaverFolder(SettingsMenu menu) =>
        Assert.IsType<SettingFolderCardViewModel>(
            Assert.Single(menu.CreateFolder().GetChildren(), card => card.Title == "Screensaver"));
}
