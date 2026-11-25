namespace HoloNet.TvLauncher.Views;

/// <summary>
/// One title in a streaming folder: the card shown for it (a show's season folder, or a film)
/// and the id it's stored under in HoloNet.Video's library, so the player can remove it again
/// (see <see cref="FolderNavigator.TryGetLibraryTitle"/>).
/// </summary>
public sealed record StreamingTitle(string LibraryId, IPickerCard Card);