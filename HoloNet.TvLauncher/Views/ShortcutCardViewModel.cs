using HoloNet.TvLauncher.Configuration;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Presentation wrapper around a configured <see cref="ShortcutMapping"/> for binding in the
/// picker, alongside <see cref="GameCardViewModel"/> (see <see cref="IPickerCard"/>). Unlike a
/// game, launching one opens <see cref="Shortcut"/>'s URL in a browser (the default one, or
/// kiosk-mode Firefox - see <see cref="ShortcutMapping.Kiosk"/>) — there's no save stats or
/// showcase screenshot.
/// </summary>
public class ShortcutCardViewModel(ShortcutMapping shortcut) : PickerCardBase
{
    public ShortcutMapping Shortcut { get; } = shortcut;

    public override string Title => Shortcut.Title;

    public override string Subtitle => Shortcut.Kiosk ? "App · Ad-free" : "App";

    public override string? Description => Shortcut.Description;

    public override string? ThumbnailUrl => Shortcut.ThumbnailUrl;

    public override string? IconGlyph => TileIcons.FromName(Shortcut.Icon);

    public override string AccentColor => string.IsNullOrWhiteSpace(Shortcut.AccentColor) ? DefaultAccentColor : Shortcut.AccentColor;

    public override TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor) => visitor.VisitShortcut(this);
}
