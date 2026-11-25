using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace HoloNet.Video.Providers.DisneyPlus;

/// <summary>A Disney+ series as described by its public, signed-out series page.</summary>
internal sealed record DisneyPlusSeriesPage(
    string Title,
    string? Synopsis,
    int? Year,
    IReadOnlyList<string> Genres,
    string? BackgroundImageId,
    IReadOnlyList<DisneyPlusSeason> Seasons,
    string? SelectedSeasonId,
    IReadOnlyList<DisneyPlusEpisode> SelectedSeasonEpisodes);

internal sealed record DisneyPlusSeason(string Id, string Name, int Number);

internal sealed record DisneyPlusEpisode(string Id, string Title, int? Number, string? Summary, string? ImageId);

/// <summary>
/// Reads a Disney+ series page's server-rendered Next.js data (the <c>__NEXT_DATA__</c> script):
/// the page is built from typed "stitch" components, of which only the hero (artwork, synopsis,
/// years, genres), the media details (title) and the episodes list (seasons plus the selected
/// season's episodes) are modelled. The page embeds one season's episodes at a time; the
/// selected season is chosen with the page's own <c>?season={seasonId}</c> query.
/// </summary>
internal static partial class DisneyPlusPageParser
{
    private const string NextDataStart = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
    private const string ScriptEnd = "</script>";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <returns>The parsed page, or <c>null</c> when it isn't a series page with a title.</returns>
    public static DisneyPlusSeriesPage? Parse(string html)
    {
        var components = ReadComponents(html);
        if (components is null)
        {
            return null;
        }

        var hero = Find<HeroComponent>(components, "DetailEntityHero");
        var details = Find<MediaDetailsComponent>(components, "MediaDetails");
        var episodes = Find<EpisodesComponent>(components, "Episodes");

        var title = FirstNonEmpty(details?.Title, episodes?.SeriesTitle, hero?.BackgroundImage?.Alt);
        if (title is null)
        {
            return null;
        }

        return new DisneyPlusSeriesPage(
            Title: title,
            Synopsis: FirstNonEmpty(hero?.SynopsisText, details?.Summary),
            Year: ParseFirstYear(hero?.ReleaseYear ?? details?.Release),
            Genres: hero?.Genres ?? details?.Genres ?? [],
            BackgroundImageId: hero?.BackgroundImage?.DefaultImage?.RipcutId,
            Seasons: ToSeasons(episodes?.Seasons),
            SelectedSeasonId: episodes?.SelectedSeasonId,
            SelectedSeasonEpisodes: ToEpisodes(episodes?.Episodes));
    }

    private static List<JsonElement>? ReadComponents(string html)
    {
        var start = html.IndexOf(NextDataStart, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += NextDataStart.Length;
        var end = html.IndexOf(ScriptEnd, start, StringComparison.Ordinal);
        if (end < 0)
        {
            return null;
        }

        try
        {
            var nextData = JsonSerializer.Deserialize<NextData>(html.AsSpan(start, end - start), SerializerOptions);
            return nextData?.Props?.PageProps?.StitchDocument?.MainContent;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static T? Find<T>(List<JsonElement> components, string type) where T : class
    {
        foreach (var component in components)
        {
            if (component.ValueKind == JsonValueKind.Object
                && component.TryGetProperty("_type", out var typeProperty)
                && typeProperty.ValueKind == JsonValueKind.String
                && typeProperty.GetString() == type)
            {
                return component.Deserialize<T>(SerializerOptions);
            }
        }

        return null;
    }

    private static List<DisneyPlusSeason> ToSeasons(List<SeasonTab>? tabs) =>
        (tabs ?? [])
            .Where(tab => !string.IsNullOrWhiteSpace(tab.Id) && !string.IsNullOrWhiteSpace(tab.Name))
            .Select((tab, index) => new DisneyPlusSeason(tab.Id!, tab.Name!, ParseFirstNumber(tab.Name) ?? index + 1))
            .ToList();

    private static List<DisneyPlusEpisode> ToEpisodes(List<ImageCard>? cards) =>
        (cards ?? [])
            .Where(card => !string.IsNullOrWhiteSpace(card.Id) && !string.IsNullOrWhiteSpace(card.Title))
            .Select(card =>
            {
                var (number, title) = SplitEpisodeTitle(card.Title!);
                return new DisneyPlusEpisode(card.Id!, title, number, card.Metadata?.Summary, card.ImageVariants?.DefaultImage?.RipcutId);
            })
            .ToList();

    /// <summary>Splits an episode card title like "S1:E2 Rising Malevolence" into its episode
    /// number and its own title; a title in any other format is kept whole, without a number.</summary>
    private static (int? Number, string Title) SplitEpisodeTitle(string cardTitle)
    {
        var match = EpisodeTitlePattern().Match(cardTitle);
        return match.Success
            ? (int.Parse(match.Groups["episode"].Value, CultureInfo.InvariantCulture), match.Groups["title"].Value)
            : (null, cardTitle);
    }

    private static int? ParseFirstYear(string? text) =>
        text is not null && YearPattern().Match(text) is { Success: true } match
            ? int.Parse(match.Value, CultureInfo.InvariantCulture)
            : null;

    private static int? ParseFirstNumber(string? text) =>
        text is not null && NumberPattern().Match(text) is { Success: true } match
            && int.TryParse(match.Value, CultureInfo.InvariantCulture, out var number)
            ? number
            : null;

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    [GeneratedRegex(@"^S\d+:E(?<episode>\d+)\s+(?<title>.+)$")]
    private static partial Regex EpisodeTitlePattern();

    [GeneratedRegex(@"\b\d{4}\b")]
    private static partial Regex YearPattern();

    [GeneratedRegex(@"\d+")]
    private static partial Regex NumberPattern();

    // Narrow view of the page's Next.js data - only the catalog components listed above.
    private sealed record NextData(NextDataProps? Props);

    private sealed record NextDataProps(PageProps? PageProps);

    private sealed record PageProps(StitchDocument? StitchDocument);

    private sealed record StitchDocument(List<JsonElement>? MainContent);

    private sealed record HeroComponent(Image? BackgroundImage, string? SynopsisText, string? ReleaseYear, List<string>? Genres);

    private sealed record MediaDetailsComponent(string? Title, string? Summary, string? Release, List<string>? Genres);

    private sealed record EpisodesComponent(string? SeriesTitle, List<SeasonTab>? Seasons, string? SelectedSeasonId, List<ImageCard>? Episodes);

    private sealed record SeasonTab(string? Id, string? Name);

    private sealed record ImageCard([property: JsonPropertyName("_id")] string? Id, string? Title, Image? ImageVariants, CardMetadata? Metadata);

    private sealed record CardMetadata(string? Summary);

    private sealed record Image(string? Alt, ImageSource? DefaultImage);

    private sealed record ImageSource(string? RipcutId);
}
