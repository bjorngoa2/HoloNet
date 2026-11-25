namespace HoloNet.Video.Models;

/// <summary>The outcome of <see cref="Services.ILibraryService.ImportAsync"/>.</summary>
public enum LibraryImportStatus
{
    Imported,
    AlreadyInLibrary,
    UnrecognisedLink,
    TitleNotFound,
    ProviderUnavailable,
}

/// <summary>
/// The outcome of importing a title. <see cref="Title"/> is set when the title is (now) in the
/// library; <see cref="Message"/> explains anything else in words fit to show the user.
/// </summary>
public sealed record LibraryImportResult(LibraryImportStatus Status, LibraryTitleDto? Title, string Message);
