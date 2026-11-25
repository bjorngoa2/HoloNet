using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies <see cref="FolderNavigator"/>'s Prime Video refresh-while-browsing behavior: a
/// season/episode-list folder's children factory closes over a fixed <see cref="MediaItemDto"/>
/// snapshot taken when it was created (see <see cref="MediaCardViewModel.Create"/>), so
/// <see cref="FolderNavigator.SetStreamingItems"/> must re-walk the same drill-in path against
/// the fresh tree (rather than re-invoking the stale current folder's own
/// <see cref="FolderCardViewModel.GetChildren"/>) for progress/resume state to ever update while
/// the player is sitting inside an episode list.
/// </summary>
public class FolderNavigatorShould
{
    private static readonly StreamingService Prime = StreamingService.PrimeVideo;

    private static TvLauncherOptions Options() => new() { PrimeVideoEnabled = true };

    private static FolderNavigator Navigator(TvLauncherOptions options, FakeLauncherSettingsStore? settingsStore = null)
    {
        settingsStore ??= new FakeLauncherSettingsStore();
        return new(options, settingsStore, new SettingsMenu(options, settingsStore, previewScreensaver: () => { }, currentVersion: "1.0.0"));
    }

    /// <summary>Games, both streaming services and two shortcuts, then Settings.</summary>
    private static TvLauncherOptions HomeOptions() => new()
    {
        PrimeVideoEnabled = true,
        DisneyPlusEnabled = true,
        Shortcuts =
        [
            new ShortcutMapping { Title = "YouTube", Url = "https://youtube.com" },
            new ShortcutMapping { Title = "Spotify", Url = "https://spotify.com" }
        ]
    };

    private static IEnumerable<string> HomeTitles(FolderNavigator navigator) => navigator.Cards.Select(c => c.Title);

    private static MediaItemDto Episode(string title, int number, double? percentWatched = null) =>
        new($"id-ep-{number}", title, EntityType: 3, PlaybackType: 1, Description: null, PosterUrl: null,
            BackdropUrl: null, Year: null, Genres: null, EntitlementMessage: null, DeepLinkUrl: $"https://example.test/ep/{number}",
            SequenceNumber: number, Progress: percentWatched is null ? null : new MediaProgressDto(percentWatched.Value, null, null));

    private static MediaItemDto Season(string id, string title, int number, IReadOnlyList<MediaItemDto> episodes) =>
        new(id, title, EntityType: 2, PlaybackType: 1, Description: null, PosterUrl: null,
            BackdropUrl: null, Year: null, Genres: null, EntitlementMessage: null, DeepLinkUrl: $"https://example.test/season/{number}",
            SequenceNumber: number, Episodes: episodes);

    [Fact]
    public void RefreshEpisodeProgress_WhileBrowsingItsEpisodeListFolder()
    {
        var navigator = Navigator(Options());
        var season = Season("show-1", "Clarkson's Farm - Sesong 1", 1, [Episode("Tractoring", 1, percentWatched: 0)]);

        navigator.SetStreamingItems(Prime, [Title(season, Prime)]);

        // Drill: root -> "Prime Video" folder -> the season's own episode-list folder.
        var primeVideoFolder = Assert.IsType<FolderCardViewModel>(navigator.Cards.Single(c => c is FolderCardViewModel { Title: "Prime Video" }));
        navigator.EnterFolder(primeVideoFolder, selectedIndex: 0);
        var seasonFolder = Assert.IsType<FolderCardViewModel>(navigator.Cards.Single(c => c is FolderCardViewModel { Title: "Clarkson's Farm - Sesong 1" }));
        navigator.EnterFolder(seasonFolder, selectedIndex: 0);

        var episodeBefore = Assert.IsType<MediaCardViewModel>(navigator.Cards.Single());
        Assert.Equal(0, episodeBefore.ProgressPercentage);

        // Simulate a refresh after watching more - a brand new MediaItemDto tree with updated progress.
        var refreshedSeason = Season("show-1", "Clarkson's Farm - Sesong 1", 1, [Episode("Tractoring", 1, percentWatched: 44.3)]);
        navigator.SetStreamingItems(Prime, [Title(refreshedSeason, Prime)]);

        var episodeAfter = Assert.IsType<MediaCardViewModel>(navigator.Cards.Single());
        Assert.Equal(44.3, episodeAfter.ProgressPercentage);
        Assert.Equal("Clarkson's Farm - Sesong 1", navigator.HeaderTitle);
    }

    [Fact]
    public void FallBackToTheDeepestResolvableAncestor_WhenATitleLeavesTheWatchlist()
    {
        var navigator = Navigator(Options());
        var season = Season("show-1", "Clarkson's Farm - Sesong 1", 1, [Episode("Tractoring", 1)]);

        navigator.SetStreamingItems(Prime, [Title(season, Prime)]);
        var primeVideoFolder = Assert.IsType<FolderCardViewModel>(navigator.Cards.Single(c => c is FolderCardViewModel { Title: "Prime Video" }));
        navigator.EnterFolder(primeVideoFolder, selectedIndex: 0);
        var seasonFolder = Assert.IsType<FolderCardViewModel>(navigator.Cards.Single(c => c is FolderCardViewModel { Title: "Clarkson's Farm - Sesong 1" }));
        navigator.EnterFolder(seasonFolder, selectedIndex: 0);

        // The title is gone from the refreshed watchlist entirely.
        navigator.SetStreamingItems(Prime, []);

        // Can no longer resolve the season folder, so we land back on the (still-valid) "Prime
        // Video" folder rather than erroring or silently showing stale data.
        Assert.Equal("Prime Video", navigator.HeaderTitle);
    }

    [Fact]
    public void LeaveGamesBrowsingUnaffected_ByAPrimeVideoRefresh()
    {
        var navigator = Navigator(Options());
        navigator.SetStreamingItems(Prime, []);

        var gamesFolder = Assert.IsType<FolderCardViewModel>(navigator.Cards.Single(c => c is FolderCardViewModel { Title: "Games" }));
        navigator.EnterFolder(gamesFolder, selectedIndex: 0);
        Assert.Equal("Games", navigator.HeaderTitle);

        navigator.SetStreamingItems(Prime, []);

        Assert.Equal("Games", navigator.HeaderTitle);
    }

    [Fact]
    public void ShowAFolderPerEnabledStreamingService_WithItsOwnTitles()
    {
        var navigator = Navigator(new TvLauncherOptions { PrimeVideoEnabled = true, DisneyPlusEnabled = true });
        var season = Season("clone-wars-s1", "Season 1", 1, [Episode("Ambush", 1)]);

        navigator.SetStreamingItems(StreamingService.DisneyPlus, [Title(season, StreamingService.DisneyPlus)]);

        var disneyFolder = Assert.IsType<FolderCardViewModel>(navigator.Cards.Single(c => c is FolderCardViewModel { Title: "Disney+" }));
        Assert.Equal("1 title", disneyFolder.Subtitle);
        Assert.Equal("Login required", navigator.Cards.Single(c => c.Title == "Prime Video").Subtitle);

        navigator.EnterFolder(disneyFolder, selectedIndex: 0);
        Assert.Collection(
            navigator.Cards,
            card => Assert.Equal(StreamingService.DisneyPlus.AccentColor, Assert.IsType<FolderCardViewModel>(card).AccentColor),
            card => Assert.Same(StreamingService.DisneyPlus, Assert.IsType<AddTitleActionCardViewModel>(card).Service));
    }

    [Fact]
    public void FindTheLibraryTitle_OnlyAmongTheTitlesInItsServiceFolder()
    {
        var navigator = Navigator(Options());
        var title = Title(Season("show-1", "Clarkson's Farm - Sesong 1", 1, [Episode("Tractoring", 1)]), Prime);
        navigator.SetStreamingItems(Prime, [title]);

        Assert.False(navigator.TryGetLibraryTitle(title.Card, out _, out _));

        navigator.EnterFolder(Assert.IsType<FolderCardViewModel>(navigator.Cards.Single(c => c.Title == "Prime Video")), selectedIndex: 0);
        var titleCard = navigator.Cards.Single(c => c.Title == "Clarkson's Farm - Sesong 1");
        Assert.True(navigator.TryGetLibraryTitle(titleCard, out var service, out var found));
        Assert.Same(Prime, service);
        Assert.Same(title, found);
        Assert.False(navigator.TryGetLibraryTitle(navigator.Cards.OfType<AddTitleActionCardViewModel>().Single(), out _, out _));

        navigator.EnterFolder(Assert.IsType<FolderCardViewModel>(titleCard), selectedIndex: 0);
        Assert.False(navigator.TryGetLibraryTitle(navigator.Cards.Single(), out _, out _));
    }

    [Fact]
    public void EndTheHomeScreenWithSettings()
    {
        var navigator = Navigator(Options());
        navigator.SetGames([]);

        var settings = Assert.IsType<FolderCardViewModel>(navigator.Cards[^1]);
        Assert.Equal("Settings", settings.Title);
    }

    [Fact]
    public void ArrangeHome_InTheSavedOrder_WithNewTilesAfterAndSettingsLast()
    {
        var settingsStore = new FakeLauncherSettingsStore(new LauncherSettings(
            HomeOrder: ["shortcut:Spotify", StreamingService.DisneyPlus.RootNavigationKey, "no-longer-here", "games"]));
        var navigator = Navigator(HomeOptions(), settingsStore);

        navigator.SetGames([]);

        Assert.Equal(["Spotify", "Disney+", "Games", "Prime Video", "YouTube", "Settings"], HomeTitles(navigator));
    }

    [Fact]
    public async Task MoveATile_AndSaveTheNewOrder()
    {
        var settingsStore = new FakeLauncherSettingsStore();
        var navigator = Navigator(HomeOptions(), settingsStore);
        navigator.SetGames([]);

        navigator.BeginMove(index: 3);
        Assert.Equal(3, navigator.MovingIndex);
        Assert.Equal(0, navigator.MoveTo(0));
        var index = await navigator.CommitMoveAsync();

        Assert.Equal(0, index);
        Assert.False(navigator.IsMoving);
        Assert.All(navigator.Cards, card => Assert.False(card.IsMoving));
        Assert.Equal(["YouTube", "Games", "Prime Video", "Disney+", "Spotify", "Settings"], HomeTitles(navigator));
        Assert.Equal(
            ["shortcut:YouTube", "games", Prime.RootNavigationKey, StreamingService.DisneyPlus.RootNavigationKey, "shortcut:Spotify"],
            Assert.Single(settingsStore.Saved).HomeOrder);
    }

    [Fact]
    public void KeepSettingsLast_AndUnmovable()
    {
        var navigator = Navigator(HomeOptions());
        navigator.SetGames([]);

        Assert.False(navigator.CanMove(navigator.Cards.Count - 1));

        navigator.BeginMove(index: 0);
        Assert.Equal(navigator.Cards.Count - 2, navigator.MoveTo(navigator.Cards.Count - 1));
        Assert.Equal("Settings", navigator.Cards[^1].Title);
    }

    [Fact]
    public void PutEverythingBack_WhenAMoveIsCancelled()
    {
        var settingsStore = new FakeLauncherSettingsStore();
        var navigator = Navigator(HomeOptions(), settingsStore);
        navigator.SetGames([]);

        navigator.BeginMove(index: 1);
        navigator.MoveTo(4);

        Assert.Equal(1, navigator.CancelMove());
        Assert.False(navigator.IsMoving);
        Assert.Equal(["Games", "Prime Video", "Disney+", "YouTube", "Spotify", "Settings"], HomeTitles(navigator));
        Assert.Empty(settingsStore.Saved);
    }

    [Fact]
    public void KeepTheTileBeingMoved_WhereItIs_WhenARefreshRebuildsHome()
    {
        var navigator = Navigator(HomeOptions());
        navigator.SetGames([]);
        navigator.BeginMove(index: 0);
        navigator.MoveTo(2);

        navigator.SetStreamingItems(Prime, []);

        Assert.Equal(2, navigator.MovingIndex);
        Assert.Equal("Games", navigator.Cards[2].Title);
    }

    [Fact]
    public void OnlyMoveTiles_OnTheHomeScreen()
    {
        var navigator = Navigator(HomeOptions());
        navigator.SetGames([]);

        navigator.EnterFolder(Assert.IsType<FolderCardViewModel>(navigator.Cards[1]), selectedIndex: 1);

        Assert.False(navigator.CanMove(0));
    }

    private static StreamingTitle Title(MediaItemDto item, StreamingService service) =>
        new(item.ProviderMediaId, MediaCardViewModel.Create(item, service));
}
