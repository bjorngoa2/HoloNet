namespace HoloNet.TvLauncher.Views;

/// <summary>
/// The "Add a title" tile at the end of a streaming folder (see <see cref="FolderNavigator"/>).
/// Selecting it asks for a link to a show or film and adds it to HoloNet.Video's library (see
/// <see cref="Services.IMediaLibraryApiClient"/>), so the folder's contents are managed from
/// the launcher itself.
/// </summary>
public class AddTitleActionCardViewModel(StreamingService service) : PickerCardBase
{
    public StreamingService Service { get; } = service;

    public override string Title => "Add a title";

    public override string Subtitle => Service.Name;

    public override string Description =>
        $"Add a show or film to {Service.Name} by pasting a link to it, copied from the {Service.Name} website.";

    public override string IconGlyph => TileIcons.Add;

    public override string AccentColor => Service.AccentColor;

    public override string ActionLabel => "Add";

    public override TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor) => visitor.VisitAddTitleAction(this);
}