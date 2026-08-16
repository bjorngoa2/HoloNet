using System.ComponentModel;
using System.Runtime.CompilerServices;
using HoloNet.TvLauncher.Models;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Presentation wrapper around <see cref="GameDto"/> for binding in the picker grid.
/// </summary>
public class GameCardViewModel(GameDto game, SaveStats? saveStats = null) : INotifyPropertyChanged
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

    public string InitialsGlyph => string.Concat(Game.Title
        .Split(' ', StringSplitOptions.RemoveEmptyEntries)
        .Take(2)
        .Select(word => char.ToUpperInvariant(word[0])));

    public string? ThumbnailUrl => Game.ThumbnailUrl;

    public bool HasThumbnail => !string.IsNullOrWhiteSpace(Game.ThumbnailUrl);

    public bool ShowInitials => !HasThumbnail;

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
