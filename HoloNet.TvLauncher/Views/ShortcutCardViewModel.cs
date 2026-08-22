using System.ComponentModel;
using System.Runtime.CompilerServices;
using HoloNet.TvLauncher.Configuration;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Presentation wrapper around a configured <see cref="ShortcutMapping"/> for binding in the
/// picker grid, alongside <see cref="GameCardViewModel"/> (see <see cref="IPickerCard"/>).
/// Unlike a game, launching one just opens <see cref="Shortcut"/>'s URL in the default browser —
/// there's no emulator process to track, no save stats, and no showcase screenshot.
/// </summary>
public class ShortcutCardViewModel(ShortcutMapping shortcut) : IPickerCard
{
    private bool _isSelected;

    public ShortcutMapping Shortcut { get; } = shortcut;

    public string Title => Shortcut.Title;

    public string Subtitle => "App";

    public string InitialsGlyph => string.Concat(Shortcut.Title
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(word => char.ToUpperInvariant(word[0])));

    public string? ThumbnailUrl => Shortcut.ThumbnailUrl;

    public bool HasThumbnail => !string.IsNullOrWhiteSpace(Shortcut.ThumbnailUrl);

    public bool ShowInitials => !HasThumbnail;

    public bool IsFolder => false;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
                return;

            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
