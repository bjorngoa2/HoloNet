using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Shared implementation of <see cref="IPickerCard"/>: selection state, change notification,
/// and the derived "which artwork layer to show" flags - so each concrete card only describes
/// its own data. WPF binds these by reflection, which (unlike C# callers) can't see interface
/// default members, so they live here as real class members instead.
/// </summary>
public abstract class PickerCardBase : IPickerCard
{
    /// <summary>HoloNet's own brand blue, for tiles that don't have a brand colour of their own.</summary>
    public const string DefaultAccentColor = "#3D6BFF";

    private bool _isSelected;
    private bool _isMoving;

    public abstract string Title { get; }

    public abstract string Subtitle { get; }

    public virtual string? Description => null;

    public virtual string? Details => null;

    public virtual string? PreviewImagePath => null;

    public virtual string? ThumbnailUrl => null;

    public virtual string? BackdropUrl => null;

    public virtual string? IconGlyph => null;

    public virtual string AccentColor => DefaultAccentColor;

    public virtual CardShape Shape => CardShape.Banner;

    public virtual bool CaptionsArt => false;

    public virtual string ActionLabel => "Open";

    public virtual double? ProgressPercentage => null;

    public bool HasThumbnail => !string.IsNullOrWhiteSpace(ThumbnailUrl);

    public bool ShowIcon => !HasThumbnail && IconGlyph is not null;

    public bool ShowInitials => !HasThumbnail && IconGlyph is null;

    public string InitialsGlyph => string.Concat(Title
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(word => char.ToUpperInvariant(word[0])));

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

    public bool IsMoving
    {
        get => _isMoving;
        set
        {
            if (_isMoving == value)
                return;

            _isMoving = value;
            OnPropertyChanged();
        }
    }

    public abstract TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
