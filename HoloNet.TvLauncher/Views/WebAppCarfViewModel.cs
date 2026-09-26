using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HoloNet.TvLauncher.Views;

public sealed class WebAppCardViewModel(string title, string url, string? thumbnailUrl) : IPickerCard
{
    private bool _isSelected;
    public string Title { get; } = title;
    public string Subtitle => "Web App";
    public string? ThumbnailUrl { get; } = thumbnailUrl;
    public bool HasThumbnail => !string.IsNullOrWhiteSpace(ThumbnailUrl);
    public bool ShowInitials => !HasThumbnail;
    public string InitialsGlyph => string.Concat(Title.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(2).Select(word => char.ToUpperInvariant(word[0])));
    public bool IsFolder  => false;
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
    public string Url { get; set; } = url;

    public TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor) => visitor.VisitWebApp(this);
    public event PropertyChangedEventHandler? PropertyChanged;
    
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>  PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}