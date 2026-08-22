using System.ComponentModel;
using System.Runtime.CompilerServices;
using HoloNet.TvLauncher.Models;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Presentation wrapper around <see cref="GameDto"/> for binding in the picker grid.
/// </summary>
public class GameCardViewModel(GameDto game, SaveStats? saveStats = null) : IPickerCard
{
    private bool _isSelected;

    private SaveStats? _saveStats = saveStats;

    public GameDto Game { get; } = game;

    public SaveStats? SaveStats
    {
        get => _saveStats;
        set
        {
            _saveStats = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SaveStatsText));
            OnPropertyChanged(nameof(HasSaveStats));
        }
    }

    public string Title => Game.Title;

    public string Platform => Game.Platform;

    public string YearText => Game.Year?.ToString() ?? string.Empty;

    public string Subtitle => string.IsNullOrEmpty(YearText) ? Platform : $"{Platform} · {YearText}";

    public string InitialsGlyph => string.Concat(Game.Title
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(word => char.ToUpperInvariant(word[0])));

    public string? ThumbnailUrl => Game.ThumbnailUrl;

    public bool HasThumbnail => !string.IsNullOrWhiteSpace(Game.ThumbnailUrl);

    public bool ShowInitials => !HasThumbnail;

    public bool IsFolder => false;

    private string? _showcaseScreenshotPath;

    /// <summary>
    /// Absolute path to the most recent "where I currently am in this game" screenshot captured
    /// when the player last quit this game (see <see cref="Services.GameScreenshotService"/>),
    /// or <c>null</c> if none has been captured yet. Shown as a preview overlay when this card is
    /// selected, in place of the static cover-art thumbnail.
    /// </summary>
    public string? ShowcaseScreenshotPath
    {
        get => _showcaseScreenshotPath;
        private set
        {
            _showcaseScreenshotPath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasShowcaseScreenshot));
        }
    }

    public bool HasShowcaseScreenshot => !string.IsNullOrWhiteSpace(ShowcaseScreenshotPath);

    /// <summary>
    /// Re-reads whether a showcase screenshot exists on disk for this game, e.g. after quitting
    /// it (a fresh capture may now exist) or on initial library load (a capture from a previous
    /// session may already exist).
    /// </summary>
    public void RefreshScreenshot(Services.IGameScreenshotService screenshotService) =>
        ShowcaseScreenshotPath = screenshotService.GetScreenshotPath(Game.Title);

    /// <summary>
    /// Multi-line hover-info text shown as the card's tooltip, e.g. "Bolts: 867" and
    /// "Playtime: 00:18:28". Empty when no save stats are configured/available for this game,
    /// which suppresses the tooltip entirely (see the XAML's <c>ToolTipService.IsEnabled</c> binding).
    /// </summary>
    public string SaveStatsText
    {
        get
        {
            if (SaveStats is null)
                return string.Empty;

            var lines = new List<string>();
            if (SaveStats.Currency is { } currency)
                lines.Add($"{SaveStats.CurrencyLabel}: {currency:N0}");
            if (SaveStats.Playtime is { } playtime)
                lines.Add($"Playtime: {(int)playtime.TotalHours:D2}:{playtime.Minutes:D2}:{playtime.Seconds:D2}");
            if (SaveStats.Location is { } location)
                lines.Add($"Location: {location}");
            if (SaveStats.LastPlayed is { } lastPlayed)
                lines.Add($"Last played: {lastPlayed.ToLocalTime():g}");

            return string.Join(Environment.NewLine, lines);
        }
    }

    public bool HasSaveStats => SaveStatsText.Length > 0;

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
