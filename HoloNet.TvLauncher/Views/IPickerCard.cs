using System.ComponentModel;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Common shape bound by the picker grid's shared <c>DataTemplate</c>, implemented by both
/// <see cref="GameCardViewModel"/> and <see cref="ShortcutCardViewModel"/> so games and
/// non-game shortcuts (see <see cref="Configuration.TvLauncherOptions.Shortcuts"/>) render and
/// gamepad-navigate identically in the same grid, without the UI needing a separate mode/branch
/// per item type.
/// </summary>
public interface IPickerCard : INotifyPropertyChanged
{
    string Title { get; }

    /// <summary>
    /// Secondary line shown under the title, e.g. a game's platform ("PS2") or a fixed label
    /// for shortcuts ("App").
    /// </summary>
    string Subtitle { get; }

    string? ThumbnailUrl { get; }

    bool HasThumbnail { get; }

    bool ShowInitials { get; }

    string InitialsGlyph { get; }

    /// <summary>
    /// <c>true</c> for a "drill in" tile (see <see cref="FolderCardViewModel"/>) — selecting it
    /// navigates into a child screen instead of launching anything. Used by the picker's
    /// <c>DataTemplate</c> to render folder tiles with a distinct visual style (folder glyph)
    /// so they're visually distinguishable from game/shortcut tiles at a glance.
    /// </summary>
    bool IsFolder { get; }

    bool IsSelected { get; set; }
}
