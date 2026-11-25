using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Services;
using HoloNet.TvLauncher.Views;

namespace HoloNet.TvLauncher.Tests;

/// <summary>
/// Verifies <see cref="StreamingLibrary"/> builds a folder's titles in library order, leaving
/// out any title the catalog can't fetch rather than failing the whole folder.
/// </summary>
public class StreamingLibraryShould
{
    private static readonly StreamingService Disney = StreamingService.DisneyPlus;

    [Fact]
    public async Task KeepLibraryOrder_AndSkipTitlesTheCatalogCantFetch()
    {
        var library = new FakeLibrary(["newest", "dropped", "oldest"]);
        var catalog = new FakeCatalog(["newest", "oldest"]);

        var titles = await new StreamingLibrary(library, catalog).LoadAsync(Disney);

        Assert.Equal(["newest", "oldest"], titles.Select(t => t.LibraryId));
        Assert.Equal(["Title newest", "Title oldest"], titles.Select(t => t.Card.Title));
        Assert.Equal([Disney.CatalogProvider], library.RequestedProviders);
    }

    private sealed class FakeLibrary(IReadOnlyList<string> ids) : IMediaLibraryApiClient
    {
        public List<string> RequestedProviders { get; } = [];

        public Task<IReadOnlyList<LibraryTitleDto>> GetTitlesAsync(string provider, CancellationToken cancellationToken = default)
        {
            RequestedProviders.Add(provider);
            return Task.FromResult<IReadOnlyList<LibraryTitleDto>>(
                [.. ids.Select(id => new LibraryTitleDto(provider, id, $"Title {id}", "Series", PosterUrl: null, DateTimeOffset.UnixEpoch))]);
        }

        public Task<LibraryImportResult> ImportAsync(string urlOrId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> RemoveAsync(string provider, string titleId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class FakeCatalog(IReadOnlyCollection<string> fetchableIds) : IMediaCatalogApiClient
    {
        public Task<MediaItemDto?> GetItemAsync(string provider, string titleId, CancellationToken cancellationToken = default) =>
            Task.FromResult(fetchableIds.Contains(titleId)
                ? new MediaItemDto(titleId, $"Title {titleId}", EntityType: 1, PlaybackType: 1, Description: null, PosterUrl: null,
                    BackdropUrl: null, Year: null, Genres: null, EntitlementMessage: null, DeepLinkUrl: $"https://example.test/{titleId}")
                : null);
    }
}