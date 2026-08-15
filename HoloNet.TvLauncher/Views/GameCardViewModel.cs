using System.ComponentModel;
using System.Runtime.CompilerServices;
using HoloNet.TvLauncher.Models;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Presentation wrapper around <see cref="GameDto"/> for binding in the picker grid.
/// </summary>
public class GameCardViewModel(GameDto game) : INotifyPropertyChanged
{
    private bool _isSelected;

    public GameDto Game { get; } = game;

    public string Title => Game.Title;

    public string Platform => Game.Platform;

    public string YearText => Game.Year?.ToString() ?? string.Empty;

    public string InitialsGlyph => string.Concat(Game.Title
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
