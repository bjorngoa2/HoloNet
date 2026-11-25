using HoloNet.Video.Providers.DisneyPlus;

namespace HoloNet.Video.Tests;

public class DisneyPlusPageParserShould
{
    private const string SeriesComponents = """
        {"_type":"Section","title":"Ignored"},
        {"_type":"DetailEntityHero","backgroundImage":{"alt":"Hero Title","defaultImage":{"ripcutId":"hero-art"}},
         "synopsisText":"Clones at war.","releaseYear":"2008 – 2020","genres":["Animation","Action"]},
        {"_type":"MediaDetails","title":"Star Wars: The Clone Wars","summary":"A longer summary.","release":"2008"},
        {"_type":"Episodes","seriesTitle":"Star Wars: The Clone Wars","selectedSeasonId":"s2",
         "seasons":[{"id":"s1","name":"Season 1"},{"id":"s2","name":"Season 2"},{"id":"","name":"Broken"}],
         "episodes":[
           {"_id":"e1","title":"S2:E1 Holocron Heist","imageVariants":{"defaultImage":{"ripcutId":"e1-art"}},"metadata":{"summary":"Cad Bane strikes."}},
           {"_id":"e2","title":"Bonus Featurette"},
           {"_id":"","title":"S2:E3 No Id"}]}
        """;

    [Fact]
    public void ReadTheSeriesAndTheSelectedSeasonsEpisodes()
    {
        var page = DisneyPlusPageParser.Parse(PageWith(SeriesComponents));

        Assert.NotNull(page);
        Assert.Equal("Star Wars: The Clone Wars", page.Title);
        Assert.Equal("Clones at war.", page.Synopsis);
        Assert.Equal(2008, page.Year);
        Assert.Equal(["Animation", "Action"], page.Genres);
        Assert.Equal("hero-art", page.BackgroundImageId);
        Assert.Equal([new DisneyPlusSeason("s1", "Season 1", 1), new DisneyPlusSeason("s2", "Season 2", 2)], page.Seasons);
        Assert.Equal("s2", page.SelectedSeasonId);
        Assert.Equal(
            [
                new DisneyPlusEpisode("e1", "Holocron Heist", 1, "Cad Bane strikes.", "e1-art"),
                new DisneyPlusEpisode("e2", "Bonus Featurette", null, null, null),
            ],
            page.SelectedSeasonEpisodes);
    }

    [Fact]
    public void FallBackToTheHeroArtworkTitleWithoutOtherTitles()
    {
        var page = DisneyPlusPageParser.Parse(PageWith(
            """{"_type":"DetailEntityHero","backgroundImage":{"alt":"Andor"},"releaseYear":"2022"}"""));

        Assert.NotNull(page);
        Assert.Equal("Andor", page.Title);
        Assert.Equal(2022, page.Year);
        Assert.Empty(page.Seasons);
        Assert.Empty(page.SelectedSeasonEpisodes);
    }

    [Theory]
    [InlineData("<html><body>No page data</body></html>")]
    [InlineData("""<script id="__NEXT_DATA__" type="application/json">{not json</script>""")]
    [InlineData("""<script id="__NEXT_DATA__" type="application/json">{"props":{}}</script>""")]
    public void ReturnNullForPagesWithoutSeriesData(string html) =>
        Assert.Null(DisneyPlusPageParser.Parse(html));

    [Fact]
    public void ReturnNullForASeriesPageWithoutATitle() =>
        Assert.Null(DisneyPlusPageParser.Parse(PageWith("""{"_type":"Episodes","seasons":[]}""")));

    private static string PageWith(string components) =>
        """<html><head><script id="__NEXT_DATA__" type="application/json">"""
        + """{"props":{"pageProps":{"stitchDocument":{"mainContent":["""
        + components
        + "]}}}}</script></head><body></body></html>";
}
