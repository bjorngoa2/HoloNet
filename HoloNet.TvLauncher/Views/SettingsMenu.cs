using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Builds the Settings folder at the end of the home screen: one folder per setting, holding one
/// tile per option (see <see cref="SettingChoiceCardViewModel"/>). Settings are changed with the
/// controller like everything else, and saved by <see cref="ILauncherSettingsStore"/>.
/// </summary>
/// <param name="previewScreensaver">Shows the screensaver straight after one is chosen, as a preview.</param>
public sealed class SettingsMenu(TvLauncherOptions options, ILauncherSettingsStore settingsStore, Action previewScreensaver)
{
    /// <summary>A neutral slate, so Settings doesn't compete with the content tiles' brand colours.</summary>
    public const string AccentColor = "#4A5468";

    private sealed record ScreensaverOption(ScreensaverStyle Style, string Name, string IconGlyph, string Description);

    private static readonly ScreensaverOption[] ScreensaverOptions =
    [
        new(ScreensaverStyle.AmbientClock, "Ambient clock", TileIcons.Clock,
            "A large, dimmed clock with the date and the weather. It moves to a new spot every minute, so nothing stays lit in one place."),
        new(ScreensaverStyle.BouncingLogo, "Bouncing logo", TileIcons.Tv,
            "A small, dim HoloNet logo drifting across a black screen and bouncing off the edges.")
    ];

    public FolderCardViewModel CreateFolder()
    {
        var settingCount = CreateSettings().Count;
        return new FolderCardViewModel(
            "Settings",
            $"{settingCount} setting{(settingCount == 1 ? "" : "s")}",
            CreateSettings,
            art: new CardArt(
                IconGlyph: TileIcons.Settings,
                AccentColor: AccentColor,
                Description: "Choose how HoloNet looks and behaves on this TV."));
    }

    private List<IPickerCard> CreateSettings() => [CreateScreensaverFolder()];

    private SettingFolderCardViewModel CreateScreensaverFolder() => new(
        "Screensaver",
        () => ScreensaverOptions.FirstOrDefault(o => o.Style == settingsStore.Current.Screensaver)?.Name ?? string.Empty,
        () => [.. ScreensaverOptions.Select(CreateScreensaverChoice)],
        new CardArt(
            IconGlyph: TileIcons.Screensaver,
            AccentColor: AccentColor,
            Description: $"What the TV shows after {options.ScreensaverIdleMinutes:0.#} minutes without input, to protect the screen from burn-in."));

    private SettingChoiceCardViewModel CreateScreensaverChoice(ScreensaverOption option) => new(
        option.Name,
        "Screensaver",
        $"{option.Description} Choose it to see a preview.",
        option.IconGlyph,
        () => settingsStore.Current.Screensaver == option.Style,
        async () =>
        {
            await settingsStore.SaveAsync(settingsStore.Current with { Screensaver = option.Style });
            previewScreensaver();
        });
}
