using HoloNet.TvLauncher.Models;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Presentation wrapper around <see cref="GameDto"/> for binding in the picker.
/// </summary>
public class GameCardViewModel(GameDto game, SaveStats? saveStats = null) : PickerCardBase
{
    private SaveStats? _saveStats = saveStats;

    public GameDto Game { get; } = game;

    public SaveStats? SaveStats
    {
        get => _saveStats;
        set
        {
            _saveStats = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Details));
        }
    }

    public override string Title => Game.Title;

    public string Platform => Game.Platform;

    public override string Subtitle => string.Join(" · ", new[]
    {
        Platform,
        Game.Year?.ToString(),
        Game.Genre is { Length: > 0 } genres ? string.Join(", ", genres.Take(2)) : null
    }.Where(part => !string.IsNullOrWhiteSpace(part)));

    public override string? Description => Game.Description;

    public override string? ThumbnailUrl => Game.ThumbnailUrl;

    public override string IconGlyph => TileIcons.Game;

    public override CardShape Shape => CardShape.Poster;

    public override string ActionLabel => "Play";

    private string? _previewImagePath;

    /// <summary>
    /// The most recent "where I currently am in this game" screenshot captured while the game was
    /// last played (see <see cref="Services.GameScreenshotService"/>), or <c>null</c> if none has
    /// been captured yet.
    /// </summary>
    public override string? PreviewImagePath => _previewImagePath;

    /// <summary>
    /// Re-reads whether a showcase screenshot exists on disk for this game, e.g. after quitting
    /// it (a fresh capture may now exist) or on initial library load (a capture from a previous
    /// session may already exist).
    /// </summary>
    public void RefreshScreenshot(Services.IGameScreenshotService screenshotService)
    {
        _previewImagePath = screenshotService.GetScreenshotPath(Game.Title);
        OnPropertyChanged(nameof(PreviewImagePath));
    }

    /// <summary>
    /// One-line save info for the hero panel, e.g. "Bolts: 867 · Playtime: 00:18:28", or
    /// <c>null</c> when no save stats are configured/available.
    /// </summary>
    public override string? Details
    {
        get
        {
            if (SaveStats is null)
                return null;

            var parts = new List<string>();
            if (SaveStats.Currency is { } currency)
                parts.Add($"{SaveStats.CurrencyLabel}: {currency:N0}");
            if (SaveStats.Playtime is { } playtime)
                parts.Add($"Playtime: {(int)playtime.TotalHours:D2}:{playtime.Minutes:D2}:{playtime.Seconds:D2}");
            if (SaveStats.Location is { } location)
                parts.Add($"Location: {location}");
            if (SaveStats.LastPlayed is { } lastPlayed)
                parts.Add($"Last played: {lastPlayed.ToLocalTime():g}");

            return parts.Count > 0 ? string.Join(" · ", parts) : null;
        }
    }

    public override TResult Accept<TResult>(IPickerCardVisitor<TResult> visitor) => visitor.VisitGame(this);
}
