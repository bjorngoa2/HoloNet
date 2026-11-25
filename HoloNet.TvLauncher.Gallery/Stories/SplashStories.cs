using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using HoloNet.TvLauncher.Services;
using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Gallery.Stories;

/// <summary>The full-screen splash (<see cref="SplashOverlay"/>) in each state the launcher shows it in.</summary>
public static class SplashStories
{
    private static readonly StoryCard Game = new("Ratchet & Clank: Up Your Arsenal")
    {
        Glyph = TileIcons.Game,
        Accent = "#E0542E",
        Kind = CardShape.Poster
    };

    public static IReadOnlyList<Story> All { get; } =
    [
        Splash("Launching", () => new SplashContent(Game.Title, "Starting…", Game, IsWorking: true)),
        Splash("Couldn't launch", () => new SplashContent(
            $"Couldn't open {Game.Title}",
            "The emulator closed before the game started.",
            Game,
            Hints: [ButtonPrompts.Hint(GamepadKind.Xbox, PromptButton.Confirm, "OK")])),
        Splash("Confirm · Xbox", () => ConfirmRemove(GamepadKind.Xbox)),
        Splash("Confirm · PlayStation", () => ConfirmRemove(GamepadKind.PlayStation)),
        Splash("Add a title", () => AddTitle(isWarning: false)),
        Splash("Add a title · rejected link", () => AddTitle(isWarning: true)),
        Splash("Checking for updates", () =>
            new SplashContent("Check for updates", "Looking for a newer version of HoloNet…", CheckForUpdates(), IsWorking: true)),
        Splash("Downloading an update", () =>
            new SplashContent("Check for updates", "Downloading the update… 40%", CheckForUpdates(), Progress: 0.4)),
        Splash("Update ready", () => new SplashContent(
            "Update v0.7.0 is ready",
            "HoloNet will restart to finish installing.",
            Hints:
            [
                ButtonPrompts.Hint(GamepadKind.Xbox, PromptButton.Confirm, "Install & restart"),
                ButtonPrompts.Hint(GamepadKind.Xbox, PromptButton.Cancel, "Later")
            ])),
        Splash("Installing an update", () => new SplashContent("Installing v0.7.0", "HoloNet restarts in a moment.", IsWorking: true)),
        Splash("Updated · loading", () => new SplashContent("Updated to v0.7.0", "Getting everything ready…", IsWorking: true)),
        Splash("Updated", () => new SplashContent(
            "Updated to v0.7.0",
            "HoloNet is up to date and ready to go.",
            Hints: [ButtonPrompts.Hint(GamepadKind.Xbox, PromptButton.Confirm, "OK")]))
    ];

    private static SplashContent ConfirmRemove(GamepadKind kind)
    {
        var show = new StoryCard("The Rings of Power") { Accent = StreamingService.PrimeVideo.AccentColor, Glyph = TileIcons.Video };
        return new SplashContent(
            $"Remove {show.Title}?",
            "It'll no longer be listed in Prime Video. You can add it again any time.",
            show,
            Hints:
            [
                ButtonPrompts.Hint(kind, PromptButton.Confirm, "Remove"),
                ButtonPrompts.Hint(kind, PromptButton.Cancel, "Keep")
            ]);
    }

    private static SplashContent AddTitle(bool isWarning)
    {
        var card = new AddTitleActionCardViewModel(StreamingService.PrimeVideo);
        return new SplashContent(
            card.Title,
            isWarning ? "That link isn't a Prime Video show or film." : "Paste a link to a show or film from the Prime Video website.",
            card,
            Hints:
            [
                ButtonPrompts.Hint(GamepadKind.Xbox, PromptButton.Confirm, "Add"),
                ButtonPrompts.Hint(GamepadKind.Xbox, PromptButton.Cancel, "Cancel")
            ],
            Input: new SplashInput(isWarning ? "https://example.com/not-a-show" : string.Empty, StreamingService.PrimeVideo.ExampleLink),
            IsWarning: isWarning);
    }

    private static CheckForUpdatesActionCardViewModel CheckForUpdates() => new("0.6.0");

    private static Story Splash(string name, Func<SplashContent> content) => new("Splash", name, Story.FullScreen, () =>
    {
        var splash = new SplashOverlay();
        splash.Loaded += (_, _) => splash.Show(content());

        // The launcher's background shows wherever the splash leaves it uncovered, as in the app.
        return new Grid
        {
            Width = Story.FullScreen.Width,
            Height = Story.FullScreen.Height,
            Background = (Brush)Application.Current.FindResource("BackgroundBrush"),
            Children = { splash }
        };
    });
}
