namespace HoloNet.Video.Abstractions;

/// <summary>
/// A streaming catalog whose titles the user curates by hand, since it can't be browsed or
/// searched anonymously (see <see cref="Services.ILibraryService"/>). Knows how to recognise
/// its own titles in a link copied from the streaming site, so the user can add a title by
/// pasting the address of its page.
/// </summary>
public interface ICuratedCatalogProvider : IVideoCatalogProvider
{
    /// <summary>The service's name as users know it, e.g. "Disney+".</summary>
    string DisplayName { get; }

    /// <summary>
    /// Extracts this provider's title id from a link to one of its title pages, or from a bare
    /// id; <c>null</c> when the input isn't one of this provider's titles.
    /// </summary>
    string? ParseTitleId(string urlOrId);
}
