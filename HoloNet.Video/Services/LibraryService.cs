using HoloNet.Shared.Models;
using HoloNet.Video.Abstractions;
using HoloNet.Video.Infrastructure;
using HoloNet.Video.Models;
using Microsoft.EntityFrameworkCore;

namespace HoloNet.Video.Services;

/// <summary>
/// The user's streaming library: which Prime Video and Disney+ titles the TV launcher shows in
/// each service's folder. Those sites can't be browsed or searched without signing in, so the
/// user picks titles by hand - pasting a link to a title's page - instead.
/// </summary>
public interface ILibraryService
{
    /// <summary>Every title in the library, or only <paramref name="provider"/>'s, most recently added first.</summary>
    Task<IReadOnlyList<LibraryTitleDto>> GetAllAsync(MediaProvider? provider, CancellationToken cancellationToken);

    Task<LibraryTitleDto?> GetAsync(MediaProvider provider, string providerMediaId, CancellationToken cancellationToken);

    /// <summary>
    /// Recognises which service <paramref name="urlOrId"/> links to, checks the title exists
    /// there, and adds it to the library.
    /// </summary>
    Task<LibraryImportResult> ImportAsync(string urlOrId, CancellationToken cancellationToken);

    /// <summary>Removes a title; <c>false</c> if it wasn't in the library.</summary>
    Task<bool> RemoveAsync(MediaProvider provider, string providerMediaId, CancellationToken cancellationToken);
}

public class LibraryService(
    IDbContextFactory<HoloNetVideoDbContext> dbContextFactory,
    IEnumerable<IVideoCatalogProvider> catalogProviders,
    TimeProvider timeProvider) : ILibraryService
{
    private readonly IReadOnlyList<ICuratedCatalogProvider> _curatedProviders =
        catalogProviders.OfType<ICuratedCatalogProvider>().ToList();

    public async Task<IReadOnlyList<LibraryTitleDto>> GetAllAsync(MediaProvider? provider, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var titles = await db.LibraryTitles
            .Where(title => provider == null || title.Provider == provider)
            .ToListAsync(cancellationToken);

        // Sorted here rather than in SQL: SQLite can't order by DateTimeOffset, and a hand-picked
        // library is small.
        return titles.OrderByDescending(title => title.AddedUtc).Select(ToDto).ToList();
    }

    public async Task<LibraryTitleDto?> GetAsync(MediaProvider provider, string providerMediaId, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var title = await db.LibraryTitles.FindAsync([provider, providerMediaId], cancellationToken);
        return title is null ? null : ToDto(title);
    }

    public async Task<LibraryImportResult> ImportAsync(string urlOrId, CancellationToken cancellationToken)
    {
        if (Recognise(urlOrId) is not ({ } provider, { } id))
        {
            return new LibraryImportResult(LibraryImportStatus.UnrecognisedLink, null,
                $"Paste a link to a title on {string.Join(" or ", _curatedProviders.Select(p => p.DisplayName))}.");
        }

        var existing = await GetAsync(provider.Provider, id, cancellationToken);
        if (existing is not null)
        {
            return AlreadyInLibrary(existing, provider);
        }

        MediaItem? item;
        try
        {
            item = await provider.GetAsync(id, cancellationToken);
        }
        catch (HttpRequestException)
        {
            return new LibraryImportResult(LibraryImportStatus.ProviderUnavailable, null,
                $"{provider.DisplayName} isn't responding. Try again in a moment.");
        }

        if (item is null)
        {
            return new LibraryImportResult(LibraryImportStatus.TitleNotFound, null,
                $"Couldn't find that title on {provider.DisplayName}. Check the link, or refresh your login if the service needs one.");
        }

        var title = new LibraryTitle
        {
            Provider = provider.Provider,
            ProviderMediaId = id,
            Title = item.Title,
            EntityType = item.EntityType,
            PosterUrl = item.PosterUrl,
            AddedUtc = timeProvider.GetUtcNow(),
        };

        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        db.LibraryTitles.Add(title);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Lost a race with a concurrent import of the same title.
            var addedMeanwhile = await GetAsync(provider.Provider, id, cancellationToken);
            if (addedMeanwhile is null)
            {
                throw;
            }

            return AlreadyInLibrary(addedMeanwhile, provider);
        }

        return new LibraryImportResult(LibraryImportStatus.Imported, ToDto(title), $"Added {title.Title} to {provider.DisplayName}.");
    }

    public async Task<bool> RemoveAsync(MediaProvider provider, string providerMediaId, CancellationToken cancellationToken)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
        var removed = await db.LibraryTitles
            .Where(title => title.Provider == provider && title.ProviderMediaId == providerMediaId)
            .ExecuteDeleteAsync(cancellationToken);
        return removed > 0;
    }

    /// <summary>Which service <paramref name="urlOrId"/> belongs to, and its id there.</summary>
    private (ICuratedCatalogProvider Provider, string Id)? Recognise(string urlOrId)
    {
        if (string.IsNullOrWhiteSpace(urlOrId))
        {
            return null;
        }

        foreach (var provider in _curatedProviders)
        {
            if (provider.ParseTitleId(urlOrId) is { } id)
            {
                return (provider, id);
            }
        }

        return null;
    }

    private static LibraryImportResult AlreadyInLibrary(LibraryTitleDto title, ICuratedCatalogProvider provider) =>
        new(LibraryImportStatus.AlreadyInLibrary, title, $"{title.Title} is already in {provider.DisplayName}.");

    private static LibraryTitleDto ToDto(LibraryTitle title) =>
        new(title.Provider, title.ProviderMediaId, title.Title, title.EntityType, title.PosterUrl, title.AddedUtc);
}
