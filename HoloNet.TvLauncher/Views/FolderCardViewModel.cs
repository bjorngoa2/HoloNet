using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// A "drill in" tile in the picker grid (see <see cref="IPickerCard"/>) — e.g. the root
/// screen's "Games" folder, or a platform folder like "PS2" underneath it. Selecting one
/// navigates into a child screen (see <see cref="MainWindow.LaunchSelectedAsync"/>'s
/// folder-navigation branch) instead of launching anything.
/// </summary>
/// <param name="childrenFactory">
/// Lazily builds this folder's contents. Deferred (rather than built eagerly for every folder up
/// front) so, e.g., a platform folder's game-card list isn't constructed until the player
/// actually opens it.
/// </param>
public class FolderCardViewModel(string title, string subtitle, Func<List<IPickerCard>> childrenFactory) : IPickerCard
{
    private bool _isSelected;

    public string Title { get; } = title;

    public string Subtitle { get; } = subtitle;

    public string? ThumbnailUrl => null;

    public bool HasThumbnail => false;

    public bool ShowInitials => true;

    // A folder-shaped glyph rather than initials, so it reads as "container" rather than
    // "item" even before the card style's own folder-icon treatment kicks in.
    public string InitialsGlyph => "\U0001F4C1";

    public bool IsFolder => true;

    public List<IPickerCard> GetChildren() => childrenFactory();

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
