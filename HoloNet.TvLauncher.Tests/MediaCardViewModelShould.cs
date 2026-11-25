using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies <see cref="MediaCardViewModel.Create"/> picks the right drill-in shape for a Prime
/// Video item: a leaf card for a movie/episode/single-season show, or a folder for a
/// multi-season show (drilling to its seasons) or a season (drilling to its episodes) - see
/// <see cref="MediaCardViewModel.Create"/>'s doc comment for the reasoning.
/// </summary>
public class MediaCardViewModelShould
{
    private static MediaItemDto Movie(string title = "Drawn together") =>
        new("id-movie", title, EntityType: 0, PlaybackType: 1, Description: null, PosterUrl: null,
            BackdropUrl: null, Year: 2010, Genres: null, EntitlementMessage: null, DeepLinkUrl: "https://example.test/movie");

    private static MediaItemDto Episode(string title, int number) =>
        new($"id-ep-{number}", title, EntityType: 3, PlaybackType: 1, Description: null, PosterUrl: null,
            BackdropUrl: null, Year: null, Genres: null, EntitlementMessage: null, DeepLinkUrl: $"https://example.test/ep/{number}",
            SequenceNumber: number);

    private static MediaItemDto Season(string title, int number, IReadOnlyList<MediaItemDto>? episodes = null, IReadOnlyList<MediaItemDto>? seasons = null) =>
        new($"id-season-{number}", title, EntityType: 2, PlaybackType: 1, Description: null, PosterUrl: null,
            BackdropUrl: null, Year: null, Genres: null, EntitlementMessage: null, DeepLinkUrl: $"https://example.test/season/{number}",
            SequenceNumber: number, Seasons: seasons, Episodes: episodes);

    [Fact]
    public void ReturnALeafCard_ForAMovie()
    {
        var card = MediaCardViewModel.Create(Movie(), StreamingService.PrimeVideo);

        Assert.IsType<MediaCardViewModel>(card);
    }

    [Fact]
    public void ReturnALeafCard_ForAnEpisode()
    {
        var card = MediaCardViewModel.Create(Episode("Kutt", 8), StreamingService.PrimeVideo);

        var media = Assert.IsType<MediaCardViewModel>(card);
        Assert.Equal("Episode 8", media.Subtitle);
    }

    [Fact]
    public void SkipTheSeasonsFolder_ForAShowWithOnlyOneSeason()
    {
        // Only one season overall (Seasons has exactly 1 entry) - drilling into a "Seasons"
        // folder with just one tile would be redundant, so this should go straight to that
        // season's episode folder instead.
        var episodes = new[] { Episode("Pilot", 1) };
        var lonelySeason = Season("Sesong 1", 1, episodes: episodes);
        var item = lonelySeason with { Seasons = [lonelySeason] };

        var card = MediaCardViewModel.Create(item, StreamingService.PrimeVideo);

        var folder = Assert.IsType<FolderCardViewModel>(card);
        Assert.Equal("1 episode", folder.Subtitle);
    }

    [Fact]
    public void ReturnAFolder_ForAShowWithMultipleSeasons()
    {
        var season1 = Season("Sesong 1", 1, episodes: [Episode("Pilot", 1)]);
        var season2 = Season("Sesong 2", 2, episodes: [Episode("Premiere", 1)]);
        var show = season2 with { Seasons = [season1, season2] };

        var card = MediaCardViewModel.Create(show, StreamingService.PrimeVideo);

        var folder = Assert.IsType<FolderCardViewModel>(card);
        Assert.Equal("2 seasons", folder.Subtitle);

        var children = folder.GetChildren();
        Assert.Equal(2, children.Count);
        // Ordered by SequenceNumber regardless of input order.
        Assert.All(children, c => Assert.IsType<FolderCardViewModel>(c));
    }

    [Fact]
    public void OrderSeasons_BySequenceNumber_RegardlessOfInputOrder()
    {
        var season1 = Season("Sesong 1", 1, episodes: [Episode("Pilot", 1)]);
        var season2 = Season("Sesong 2", 2, episodes: [Episode("Premiere", 1)]);
        var show = season2 with { Seasons = [season2, season1] }; // deliberately out of order

        var folder = Assert.IsType<FolderCardViewModel>(MediaCardViewModel.Create(show, StreamingService.PrimeVideo));
        var children = folder.GetChildren();

        var firstFolder = Assert.IsType<FolderCardViewModel>(children[0]);
        Assert.Equal("Sesong 1", firstFolder.Title);
    }

    [Fact]
    public void CaptionSeasonTiles_ButNotTheShowTile()
    {
        // Seasons usually share the show's artwork, so only their name tells them apart.
        var season1 = Season("Season 1", 1, episodes: [Episode("Ambush", 1)]);
        var season2 = Season("Season 2", 2, episodes: [Episode("Holocron Heist", 1)]);
        var show = Movie("The Clone Wars") with { EntityType = 1, Seasons = [season1, season2] };

        var folder = Assert.IsType<FolderCardViewModel>(MediaCardViewModel.Create(show, StreamingService.DisneyPlus));

        Assert.False(folder.CaptionsArt);
        Assert.All(folder.GetChildren(), season => Assert.True(season.CaptionsArt));
    }

    [Fact]
    public void ReturnAFolder_ForASeasonWithEpisodes()
    {
        var season = Season("Sesong 4", 4, episodes:
        [
            Episode("Kutt", 8),
            Episode("Den broderlige kjærlighets by", 1),
        ]);

        var card = MediaCardViewModel.Create(season, StreamingService.PrimeVideo);

        var folder = Assert.IsType<FolderCardViewModel>(card);
        Assert.Equal("2 episodes", folder.Subtitle);

        var children = folder.GetChildren();
        Assert.Equal(2, children.Count);
        var first = Assert.IsType<MediaCardViewModel>(children[0]);
        Assert.Equal("Den broderlige kjærlighets by", first.Title);
        Assert.Equal("Episode 1", first.Subtitle);
        Assert.Equal("Sesong 4 · Episode 1", first.Breadcrumb);
    }
}
