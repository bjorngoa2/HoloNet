using System.Net;
using HoloNet.Video.Configuration;
using HoloNet.Video.Providers.DisneyPlus;
using Microsoft.Extensions.Options;

namespace HoloNet.Video.Tests;

public class DisneyPlusProviderShould
{
    private const string SeriesUrl = "https://www.disneyplus.com/en-gb/browse/entity-series";

    [Fact]
    public async Task ShowEachSeasonsFirstEpisodeStill_OrTheSeriesArtWithoutOne()
    {
        var pages = new Dictionary<string, string>
        {
            [SeriesUrl] = SeriesPage(
                selectedSeasonId: "s1",
                """{"_id":"e1","title":"S1:E1 No Art"},{"_id":"e2","title":"S1:E2 Ambush","imageVariants":{"defaultImage":{"ripcutId":"e2-art"}}}"""),
            [$"{SeriesUrl}?season=s2"] = SeriesPage(selectedSeasonId: "s2", """{"_id":"e3","title":"S2:E1 No Art"}"""),
        };
        var provider = new DisneyPlusProvider(new StubHttpClientFactory(pages), Options.Create(new DisneyPlusOptions()));

        var series = await provider.GetAsync("series", CancellationToken.None);

        Assert.NotNull(series);
        Assert.NotNull(series.Seasons);
        Assert.Collection(
            series.Seasons,
            season1 => Assert.Contains("/e2-art/", season1.PosterUrl),
            season2 => Assert.Equal(series.PosterUrl, season2.PosterUrl));
        Assert.Contains("/hero-art/", series.PosterUrl);
    }

    private static string SeriesPage(string selectedSeasonId, string episodes) =>
        """<html><head><script id="__NEXT_DATA__" type="application/json">"""
        + """{"props":{"pageProps":{"stitchDocument":{"mainContent":["""
        + """{"_type":"DetailEntityHero","backgroundImage":{"alt":"Series","defaultImage":{"ripcutId":"hero-art"}}},"""
        + """{"_type":"Episodes","seriesTitle":"Series","selectedSeasonId":""" + $"\"{selectedSeasonId}\","
        + "\"seasons\":[{\"id\":\"s1\",\"name\":\"Season 1\"},{\"id\":\"s2\",\"name\":\"Season 2\"}],\"episodes\":["
        + episodes
        + "]}]}}}}</script></head><body></body></html>";

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
