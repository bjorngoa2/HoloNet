using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Builds a streaming folder's title cards: reads which titles the player picked from
/// HoloNet.Video's library (see <see cref="IMediaLibraryApiClient"/>), then fetches each one's
/// seasons, episodes and watch progress from the catalog (see <see cref="IMediaCatalogApiClient"/>).
/// </summary>
public sealed class StreamingLibrary(IMediaLibraryApiClient library, IMediaCatalogApiClient catalog)
{
    /// <summary>
    /// A service's titles, in library order (newest first). The catalog lookups run in
    /// parallel, and a title that can't be fetched right now is left out rather than failing
    /// the folder. Throws when the library itself can't be reached.
    /// </summary>
    public async Task<IReadOnlyList<StreamingTitle>> LoadAsync(StreamingService service, CancellationToken cancellationToken = default)
    {
        var entries = await library.GetTitlesAsync(service.CatalogProvider, cancellationToken);
        var titles = await Task.WhenAll(entries.Select(entry => LoadTitleAsync(service, entry.ProviderMediaId, cancellationToken)));
        return [.. titles.OfType<StreamingTitle>()];
    }

    /// <summary>One library title's card, or <c>null</c> if the catalog can't fetch it right now.</summary>
    public async Task<StreamingTitle?> LoadTitleAsync(StreamingService service, string libraryId, CancellationToken cancellationToken = default)
    {
        var item = await catalog.GetItemAsync(service.CatalogProvider, libraryId, cancellationToken);
        return item is null ? null : new StreamingTitle(libraryId, MediaCardViewModel.Create(item, service));
    }
}