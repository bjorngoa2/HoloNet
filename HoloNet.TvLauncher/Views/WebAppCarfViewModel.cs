using System.ComponentModel;

namespace HoloNet.TvLauncher.Views;

public sealed class WebAppCardViewModel(string title, string url) : IPickerCard
{
    public string Title { get; set; } = title;
    public string Subtitle { get; }
    public string? ThumbnailUrl { get; }
    public bool HasThumbnail { get; }
    public bool ShowInitials { get; }
    public string InitialsGlyph { get; }
    public bool IsFolder { get; }
    public bool IsSelected { get; set; }
    public string Url { get; set; } = url;

    public TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor) => visitor.VisitWebApp(this);
    public event PropertyChangedEventHandler? PropertyChanged;
}