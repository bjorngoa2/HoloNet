using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HoloNet.Video.Providers.HboMax;

/// <summary>An HBO Max show or movie as described by its public, signed-out title page.</summary>
/// <param name="Seasons">A show's seasons, in the page's order; only the page's selected season
/// lists its episodes, the others are empty. Always empty for a movie.</param>
internal sealed record HboMaxTitlePage(
    string Id,
    string Title,
    bool IsMovie,
    string? Summary,
    int? Year,
    IReadOnlyList<string> Genres,
    string? PosterUrl,
    string? BackdropUrl,
    IReadOnlyList<HboMaxSeason> Seasons);

internal sealed record HboMaxSeason(int Number, IReadOnlyList<HboMaxEpisode> Episodes);

internal sealed record HboMaxEpisode(string Id, string Title, int? Number, string? Summary, string? ImageUrl);

/// <summary>
/// Reads an HBO Max title page's server-rendered Next.js data (the <c>__NEXT_DATA__</c> script).
/// The page's content lives in <c>pageProps.pageData</c> - itself a JSON string - as a list of
/// "dynamic mappings" whose values are mostly JSON strings too; the title's own catalog record is
/// the first mapping with an <c>hbomaxId</c> and a title. A show page embeds one season's
/// episodes at a time; the selected season is chosen by the page's own <c>/s{number}</c> path.
/// </summary>
internal static partial class HboMaxPageParser
{
    private const string NextDataStart = "<script id=\"__NEXT_DATA__\" type=\"application/json\">";
    private const string ScriptEnd = "</script>";
    private const string MovieContentType = "movie";

    // The 16:9 key art with the title's logo makes the best tile; the logo-free key art makes the
    // best backdrop, since the launcher draws the title over it.
    private static readonly string[] PosterImageKeys = ["cover-artwork-horizontal", "default"];
    private static readonly string[] BackdropImageKeys = ["default", "cover-artwork-horizontal"];
    private static readonly string[] EpisodeImageKeys = ["default"];

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <returns>The parsed page, or <c>null</c> when it isn't a title page with a title.</returns>
    public static HboMaxTitlePage? Parse(string html)
    {
        var pageProps = ReadPageProps(html);
        if (pageProps?.PageData is not { } pageData)
        {
            return null;
        }

        var record = FindTitleRecord(pageData);
        var title = FirstNonEmpty(record?.Title?.Full, record?.Title?.Short);
        if (record?.HbomaxId is not { } id || !IdPattern().IsMatch(id) || title is null)
        {
            return null;
        }

        return new HboMaxTitlePage(
            Id: id.ToLowerInvariant(),
            Title: title,
            IsMovie: string.Equals(pageProps.ContentType, MovieContentType, StringComparison.OrdinalIgnoreCase),
            Summary: FirstNonEmpty(record.Summary?.Full, record.Summary?.Short),
            Year: ParseYear(record.ReleaseYear),
            Genres: record.Genres ?? [],
            PosterUrl: FirstImage(record.Images, PosterImageKeys),
            BackdropUrl: FirstImage(record.Images, BackdropImageKeys),
            Seasons: ToSeasons(record.Seasons));
    }

    private static PageProps? ReadPageProps(string html)
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
            return JsonSerializer.Deserialize<NextData>(html.AsSpan(start, end - start), SerializerOptions)?.Props?.PageProps;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static TitleRecord? FindTitleRecord(string pageDataJson)
    {
        try
        {
            var pageData = JsonSerializer.Deserialize<PageData>(pageDataJson, SerializerOptions);
            foreach (var mapping in pageData?.DynamicMappings ?? [])
            {
                if (AsObject(mapping.Value) is { } value
                    && value.TryGetProperty("hbomaxId", out var id) && id.ValueKind == JsonValueKind.String
                    && value.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.Object)
                {
                    return value.Deserialize<TitleRecord>(SerializerOptions);
                }
            }
        }
        catch (JsonException)
        {
        }

        return null;
    }

    /// <summary>A mapping's value as a JSON object, whether it's embedded as one or as a JSON string.</summary>
    private static JsonElement? AsObject(JsonElement value)
    {
        if (value.ValueKind == JsonValueKind.Object)
        {
            return value;
        }

        if (value.ValueKind != JsonValueKind.String || value.GetString() is not { } text || !text.TrimStart().StartsWith('{'))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(text);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<HboMaxSeason> ToSeasons(List<SeasonRecord>? seasons) =>
        (seasons ?? [])
            .Where(season => season.SeasonNumber is not null)
            .Select(season => new HboMaxSeason(season.SeasonNumber!.Value, ToEpisodes(season.Episodes)))
            .ToList();

    private static List<HboMaxEpisode> ToEpisodes(List<EpisodeRecord>? episodes) =>
        (episodes ?? [])
            .Select(episode => (Record: episode, Id: EpisodeId(episode.EpisodeUrl), Title: FirstNonEmpty(episode.Title?.Full, episode.Title?.Short)))
            .Where(episode => episode.Id is not null && episode.Title is not null)
            .Select(episode => new HboMaxEpisode(
                episode.Id!,
                episode.Title!,
                episode.Record.EpisodeNumber,
                FirstNonEmpty(episode.Record.Summary?.Full, episode.Record.Summary?.Short),
                FirstImage(episode.Record.Images, EpisodeImageKeys)))
            .ToList();

    /// <summary>An episode's id is the last segment of its page's URL
    /// (<c>/no/en/show/{showId}/s1/e1-pilot/{episodeId}</c>) - its catalog record has no id field.</summary>
    private static string? EpisodeId(string? episodeUrl) =>
        episodeUrl?.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() is { } segment && IdPattern().IsMatch(segment)
            ? segment.ToLowerInvariant()
            : null;

    private static string? FirstImage(Dictionary<string, JsonElement>? images, IEnumerable<string> keys) =>
        images is null
            ? null
            : keys
                .Select(key => images.TryGetValue(key, out var url) && url.ValueKind == JsonValueKind.String ? url.GetString() : null)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

    private static int? ParseYear(JsonElement? year) => year switch
    {
        { ValueKind: JsonValueKind.Number } number when number.TryGetInt32(out var value) => value,
        { ValueKind: JsonValueKind.String } text when int.TryParse(text.GetString(), NumberStyles.None, CultureInfo.InvariantCulture, out var value) => value,
        _ => null,
    };

    private static string? FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

    /// <summary>HBO Max identifies shows, movies and episodes alike by a GUID.</summary>
    [GeneratedRegex("^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", RegexOptions.IgnoreCase)]
    internal static partial Regex IdPattern();

    // Narrow view of the page's Next.js data - only the title's own catalog record.
    private sealed record NextData(NextDataProps? Props);

    private sealed record NextDataProps(PageProps? PageProps);

    private sealed record PageProps(string? ContentType, string? PageData);

    private sealed record PageData(List<DynamicMapping>? DynamicMappings);

    private sealed record DynamicMapping(JsonElement Value);

    private sealed record TitleRecord(
        string? HbomaxId,
        Text? Title,
        Text? Summary,
        JsonElement? ReleaseYear,
        List<string>? Genres,
        Dictionary<string, JsonElement>? Images,
        List<SeasonRecord>? Seasons);

    private sealed record SeasonRecord(int? SeasonNumber, List<EpisodeRecord>? Episodes);

    private sealed record EpisodeRecord(int? EpisodeNumber, Text? Title, Text? Summary, Dictionary<string, JsonElement>? Images, string? EpisodeUrl);

    private sealed record Text(string? Short, string? Full);
}
