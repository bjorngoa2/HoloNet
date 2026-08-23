namespace HoloNet.Shared.Helpers;

/// <summary>
/// Generic file-extension → HTTP content-type lookup, with a case-insensitive "is this
/// extension supported" check derived from the same map. Each media service (Games, Video,
/// Photos) used to hand-roll its own near-identical static class for this — a
/// <c>Dictionary&lt;string, string&gt;</c> keyed by extension plus an <c>IsSupported</c>/
/// <c>GetContentType</c> pair — differing only in the extension list and fallback content-type.
/// This class owns that shared lookup/fallback behavior once; each service just supplies its
/// own domain-specific extension/content-type dictionary and default.
/// </summary>
public sealed class ContentTypeMap(
    IReadOnlyDictionary<string, string> contentTypesByExtension,
    string fallbackContentType)
{
    private readonly Dictionary<string, string> _contentTypesByExtension = new(contentTypesByExtension, StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether <paramref name="filePath"/>'s extension is a known, supported type.</summary>
    public bool IsSupported(string filePath) =>
        _contentTypesByExtension.ContainsKey(Path.GetExtension(filePath));

    /// <summary>
    /// Resolves the content-type for <paramref name="filePathOrExtension"/> (a full path or a
    /// bare extension like <c>".mp4"</c> — <see cref="Path.GetExtension(string)"/> is a no-op
    /// on an already-bare extension), falling back to the configured default if unrecognized.
    /// </summary>
    public string GetContentType(string filePathOrExtension) =>
        _contentTypesByExtension.GetValueOrDefault(Path.GetExtension(filePathOrExtension), fallbackContentType);
}
