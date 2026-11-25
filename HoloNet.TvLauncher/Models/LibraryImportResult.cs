namespace HoloNet.TvLauncher.Models;

/// <summary>
/// The outcome of adding a title to the library (see
/// <see cref="Services.IMediaLibraryApiClient.ImportAsync"/>): the added title, or a message
/// fit to show the player explaining why it wasn't added.
/// </summary>
public sealed record LibraryImportResult(LibraryTitleDto? Title, string? ErrorMessage)
{
    public static LibraryImportResult Added(LibraryTitleDto title) => new(title, null);

    public static LibraryImportResult Failed(string message) => new(null, message);
}