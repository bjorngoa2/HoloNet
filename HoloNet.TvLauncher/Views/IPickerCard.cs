using System.ComponentModel;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Common shape bound by the picker's shared tile template and the hero panel (see
/// <see cref="HeroDetails"/>), so games, Prime Video titles, folders and app shortcuts render and
/// gamepad-navigate identically without the UI needing a separate mode/branch per item type.
/// Implemented via <see cref="PickerCardBase"/>.
/// </summary>
public interface IPickerCard : INotifyPropertyChanged
{
    string Title { get; }

    /// <summary>
    /// Secondary line, e.g. a game's platform and year ("PS2 · 2003") or "App" for shortcuts.
    /// Shown as the hero panel's metadata line when the card is focused.
    /// </summary>
    string Subtitle { get; }

    /// <summary>Longer synopsis shown in the hero panel when focused, if available.</summary>
    string? Description { get; }

    /// <summary>
    /// Extra one-line facts shown under the description in the hero panel (e.g. a game's save
    /// progress and playtime), or <c>null</c>.
    /// </summary>
    string? Details { get; }

    /// <summary>
    /// Absolute path to a local "where you left off" screenshot shown in the hero panel, or
    /// <c>null</c> (see <see cref="GameCardViewModel.RefreshScreenshot"/>).
    /// </summary>
    string? PreviewImagePath { get; }

    /// <summary>Tile artwork: box art, a poster, or an app logo.</summary>
    string? ThumbnailUrl { get; }

    /// <summary>
    /// Wide, high-resolution artwork for the hero backdrop. <c>null</c> falls back to a blurred
    /// <see cref="ThumbnailUrl"/>, since tile art is usually too small to show crisp full-screen.
    /// </summary>
    string? BackdropUrl { get; }

    /// <summary>A <see cref="TileIcons"/> glyph shown when there's no <see cref="ThumbnailUrl"/>.</summary>
    string? IconGlyph { get; }

    /// <summary>Hex brand colour for the tile background and the ambient glow when focused.</summary>
    string AccentColor { get; }

    CardShape Shape { get; }

    /// <summary>
    /// Whether the tile prints its <see cref="Title"/> over its artwork, for siblings that share
    /// the same image (e.g. a show's seasons) and so can't be told apart by the art alone.
    /// </summary>
    bool CaptionsArt { get; }

    /// <summary>The verb shown next to the Confirm button prompt, e.g. "Play" or "Open".</summary>
    string ActionLabel { get; }

    bool HasThumbnail { get; }

    bool ShowIcon { get; }

    bool ShowInitials { get; }

    string InitialsGlyph { get; }

    /// <summary>
    /// 0-100 "how far watched" for a title that has a provider-reported watch progress, or
    /// <c>null</c> for anything that doesn't track this (games, shortcuts, folders, and any
    /// <see cref="MediaCardViewModel"/> that hasn't been started). The tile template renders/hides
    /// its progress bar purely from this one value (see <c>ProgressConverters.cs</c>).
    /// </summary>
    double? ProgressPercentage { get; }

    bool IsSelected { get; set; }

    /// <summary>Whether the player has picked this home tile up to move it (see <see cref="FolderNavigator.BeginMove"/>).</summary>
    bool IsMoving { get; set; }

    /// <summary>
    /// Double dispatches to the matching <see cref="IPickerCardVisitor{TResult}"/> method for
    /// this card's concrete type, so callers can act on "which kind of card is this" without
    /// type-checking/downcasting (see <see cref="IPickerCardVisitor{TResult}"/>).
    /// </summary>
    TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor);
}
