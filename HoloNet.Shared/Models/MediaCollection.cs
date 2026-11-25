namespace HoloNet.Shared.Models;

/// <summary>
/// A named group of <see cref="MediaItem"/>s, e.g. a single carousel/row on a provider's home
/// page ("Continue Watching", "Sci-Fi", ...).
/// </summary>
public sealed record MediaCollection(string Title, IReadOnlyList<MediaItem> Items);
