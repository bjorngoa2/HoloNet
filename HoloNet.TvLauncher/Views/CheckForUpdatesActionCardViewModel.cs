namespace HoloNet.TvLauncher.Views;

/// <summary>
/// The "Check for updates" tile in the Settings folder (see <see cref="SettingsMenu"/>).
/// HoloNet already checks on its own (see <see cref="Services.AppUpdateTracker"/>); this is for
/// getting a just-published release onto the TV straight away.
/// </summary>
/// <param name="currentVersion">The running version, for display, e.g. "0.6.0".</param>
public sealed class CheckForUpdatesActionCardViewModel(string currentVersion) : PickerCardBase
{
    public override string Title => "Check for updates";

    public override string Subtitle => $"Version {currentVersion}";

    public override string Description =>
        $"This is HoloNet {currentVersion}. HoloNet looks for new versions on its own every few hours; check now to get a just-released one straight away.";

    public override string IconGlyph => TileIcons.Update;

    public override string AccentColor => SettingsMenu.AccentColor;

    public override string ActionLabel => "Check";

    public override TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor) => visitor.VisitCheckForUpdates(this);
}
