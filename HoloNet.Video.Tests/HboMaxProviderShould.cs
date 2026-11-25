using System.Net;
using System.Text.Json;
using HoloNet.Shared.Models;
using HoloNet.Video.Configuration;
using HoloNet.Video.Providers.HboMax;
using Microsoft.Extensions.Options;

namespace HoloNet.Video.Tests;

public class HboMaxProviderShould
{
    private const string ShowId = "c8ea8e19-cae7-4683-9b62-cdbbed744784";
    private const string MovieId = "1890c8c5-2f40-4140-a363-064428201826";
    private const string ShowUrl = $"https://www.hbomax.com/no/en/show/{ShowId}";

    [Fact]
    public async Task ReadEverySeason_FetchingTheOnesThePageDoesNotEmbed()
    {
        var pilot = Episode("1", "Pilot", "da61aef2-e4b6-441a-b2aa-91855ca05dad", image: "https://img/pilot.jpg");
        var pages = new Dictionary<string, string>
        {
            [ShowUrl] = TitlePage("series", ShowRecord(Season(1, pilot), Season(2))),
            [$"{ShowUrl}/s2"] = TitlePage("series", ShowRecord(Season(1), Season(2, Episode("1", "The Bad Fish Paradigm", "0b5c0e2a-1a1a-4b4b-8c8c-000000000002")))),
        };

        var show = await CreateProvider(pages).GetAsync(ShowId, CancellationToken.None);

        Assert.NotNull(show);
        Assert.Equal(MediaEntityType.Series, show.EntityType);
        Assert.Equal("The Big Bang Theory", show.Title);
        Assert.Equal(2007, show.Year);
        Assert.Equal("https://img/logo-art.jpg?w=1920", show.PosterUrl);
        Assert.Equal("https://img/key-art.jpg?w=1920", show.BackdropUrl);
        Assert.NotNull(show.Seasons);
        Assert.Collection(
            show.Seasons,
            season1 =>
            {
                Assert.Equal("Season 1", season1.Title);
                Assert.Equal("https://img/pilot.jpg?w=1920", season1.PosterUrl);
                var episode = Assert.Single(season1.Episodes!);
                Assert.Equal("Pilot", episode.Title);
                Assert.Equal(1, episode.SequenceNumber);
                Assert.Equal("https://play.hbomax.com/video/watch/da61aef2-e4b6-441a-b2aa-91855ca05dad", episode.DeepLinkUrl);
            },
            season2 =>
            {
                Assert.Equal($"{ShowUrl}/s2", season2.DeepLinkUrl);
                Assert.Equal(show.BackdropUrl, season2.PosterUrl);
                Assert.Equal("The Bad Fish Paradigm", Assert.Single(season2.Episodes!).Title);
            });
    }

    [Fact]
    public async Task PlayAMovieFromItsSignedInMoviePage()
    {
        var pages = new Dictionary<string, string>
        {
            [$"https://www.hbomax.com/no/en/show/{MovieId}"] = TitlePage("movie", MovieRecord()),
        };

        var movie = await CreateProvider(pages).GetAsync(MovieId, CancellationToken.None);

        Assert.NotNull(movie);
        Assert.Equal(MediaEntityType.Movie, movie.EntityType);
        Assert.Null(movie.Seasons);
        Assert.Equal($"https://play.hbomax.com/movie/{MovieId}", movie.DeepLinkUrl);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task ReturnNothing_ForAnUnknownOrInvalidId(string id) =>
        Assert.Null(await CreateProvider(new Dictionary<string, string>()).GetAsync(id, CancellationToken.None));

    private static HboMaxProvider CreateProvider(IReadOnlyDictionary<string, string> pages) =>
        new(new StubHttpClientFactory(pages), Options.Create(new HboMaxOptions()));

    /// <summary>A title page as HBO Max serves it: its catalog record is a JSON string inside a
    /// dynamic mapping, inside <c>pageData</c> - itself a JSON string.</summary>
    private static string TitlePage(string contentType, object titleRecord)
    {
        var pageData = JsonSerializer.Serialize(new
        {
            dynamicMappings = new object[]
            {
                new { value = "not a record" },
                new { value = JsonSerializer.Serialize(titleRecord) },
            },
        });
        var nextData = JsonSerializer.Serialize(new { props = new { pageProps = new { contentType, pageData } } });
        return $"""<html><head><script id="__NEXT_DATA__" type="application/json">{nextData}</script></head><body></body></html>""";
    }

    private static object ShowRecord(params object[] seasons) => new
    {
        hbomaxId = ShowId,
        title = new { full = "The Big Bang Theory" },
        summary = new { full = "Brilliant physicists." },
        releaseYear = "2007",
        genres = new[] { "Comedy" },
        images = new Dictionary<string, string>
        {
            ["default"] = "https://img/key-art.jpg",
            ["cover-artwork-horizontal"] = "https://img/logo-art.jpg",
        },
        seasons,
    };

    private static object MovieRecord() => new
    {
        hbomaxId = MovieId,
        title = new { full = "Harry Potter and the Deathly Hallows Part 2" },
        releaseYear = 2011,
        images = new Dictionary<string, string> { ["default"] = "https://img/movie.jpg" },
    };

    private static object Season(int number, params object[] episodes) => new { seasonNumber = number, episodes };

    private static object Episode(string number, string title, string id, string? image = null) => new
    {
        episodeNumber = int.Parse(number),
        title = new { full = title },
        images = image is null ? null : new Dictionary<string, string> { ["default"] = image },
        episodeUrl = $"/no/en/show/{ShowId}/s1/e{number}-{title.ToLowerInvariant().Replace(' ', '-')}/{id}",
    };

    private sealed class StubHttpClientFactory(IReadOnlyDictionary<string, string> pages) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(new StubHandler(pages));
    }

    private sealed class StubHandler(IReadOnlyDictionary<string, string> pages) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(pages.TryGetValue(request.RequestUri!.ToString(), out var html)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(html) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
    }
}
