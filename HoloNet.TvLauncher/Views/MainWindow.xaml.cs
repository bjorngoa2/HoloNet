using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Views;

public partial class MainWindow : Window
{
    // Matches CardStyle Width (220) + left/right Margin (12 each) in MainWindow.xaml.
    private const double CardFootprintWidth = 220 + 24;

    private readonly IGamesApiClient _gamesApiClient;
    private readonly IGameLauncher _gameLauncher;
    private readonly IGamepadService _gamepadService;

    private readonly List<GameCardViewModel> _cards = [];
    private int _selectedIndex;
    private bool _isBusy;

    public MainWindow(IGamesApiClient gamesApiClient, IGameLauncher gameLauncher, IGamepadService gamepadService)
    {
        InitializeComponent();

        _gamesApiClient = gamesApiClient;
        _gameLauncher = gameLauncher;
        _gamepadService = gamepadService;

        _gamepadService.ButtonPressed += OnGamepadButtonPressed;
        Loaded += async (_, _) =>
        {
            _gamepadService.AttachWindowHandle(new WindowInteropHelper(this).Handle);
            await LoadGamesAsync();
            _gamepadService.Start();
        };
        Closed += (_, _) =>
        {
            _gamepadService.Stop();
            _gamepadService.ButtonPressed -= OnGamepadButtonPressed;
        };
    }

    private async Task LoadGamesAsync()
    {
        SetStatus("Loading game library…");

        try
        {
            var games = await _gamesApiClient.GetGamesAsync();
            _cards.Clear();
            _cards.AddRange(games.Select(g => new GameCardViewModel(g)));
            GamesItemsControl.ItemsSource = _cards;

            _selectedIndex = _cards.Count > 0 ? 0 : -1;
            UpdateSelection();
            SetStatus(_cards.Count == 0
                ? "No games found. Press Start to refresh."
                : "D-pad/stick: move · A: play · Start: refresh");
        }
        catch (Exception ex)
        {
            SetStatus($"Could not reach the Games API: {ex.Message}. Press Start to retry.");
        }
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private void UpdateSelection()
    {
        for (var i = 0; i < _cards.Count; i++)
            _cards[i].IsSelected = i == _selectedIndex;
    }

    private int ColumnsPerRow()
    {
        var availableWidth = GridScrollViewer.ActualWidth;
        return Math.Max(1, (int)(availableWidth / CardFootprintWidth));
    }

    private void OnGamepadButtonPressed(object? sender, GamepadButton button) =>
        Dispatcher.Invoke(() => HandleButton(button));

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        var button = e.Key switch
        {
            Key.Up => GamepadButton.Up,
            Key.Down => GamepadButton.Down,
            Key.Left => GamepadButton.Left,
            Key.Right => GamepadButton.Right,
            Key.Enter => GamepadButton.Confirm,
            Key.Escape => GamepadButton.Cancel,
            Key.F5 => GamepadButton.Refresh,
            _ => (GamepadButton?)null
        };

        if (button is not null)
            HandleButton(button.Value);
    }

    private async void HandleButton(GamepadButton button)
    {
        if (_isBusy)
        {
            if (button is GamepadButton.Confirm or GamepadButton.Cancel && _dismissWait is not null)
                _dismissWait.TrySetResult();

            return;
        }

        if (_cards.Count == 0)
        {
            if (button == GamepadButton.Refresh)
                await LoadGamesAsync();
            return;
        }

        var columns = ColumnsPerRow();

        switch (button)
        {
            case GamepadButton.Left:
                Move(-1);
                break;
            case GamepadButton.Right:
                Move(1);
                break;
            case GamepadButton.Up:
                Move(-columns);
                break;
            case GamepadButton.Down:
                Move(columns);
                break;
            case GamepadButton.Confirm:
                await LaunchSelectedAsync();
                break;
            case GamepadButton.Refresh:
                await LoadGamesAsync();
                break;
            case GamepadButton.Cancel:
                break;
        }
    }

    private void Move(int delta)
    {
        var next = _selectedIndex + delta;
        if (next < 0 || next >= _cards.Count)
            return;

        _selectedIndex = next;
        UpdateSelection();
    }

    private async Task LaunchSelectedAsync()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _cards.Count)
            return;

        var game = _cards[_selectedIndex].Game;

        _isBusy = true;
        ShowOverlay($"Launching {game.Title}…");

        try
        {
            var launchIntent = await _gamesApiClient.GetLaunchIntentAsync(game.Id);
            if (launchIntent is null)
            {
                ShowOverlay($"\"{game.Title}\" has no network path configured and can't be launched.\n\nPress A to dismiss.");
                await WaitForDismissAsync();
                return;
            }

            var result = await _gameLauncher.LaunchAsync(launchIntent);
            if (result.Outcome != LaunchOutcome.Success)
            {
                ShowOverlay($"Couldn't launch \"{game.Title}\":\n{result.ErrorMessage}\n\nPress A to dismiss.");
                await WaitForDismissAsync();
            }
        }
        finally
        {
            HideOverlay();
            _isBusy = false;
        }
    }

    private TaskCompletionSource? _dismissWait;

    private Task WaitForDismissAsync()
    {
        _dismissWait = new TaskCompletionSource();
        var task = _dismissWait.Task;
        return task.ContinueWith(_ => _dismissWait = null, TaskScheduler.Default);
    }

    private void ShowOverlay(string message)
    {
        OverlayText.Text = message;
        OverlayGrid.Visibility = Visibility.Visible;
    }

    private void HideOverlay() => OverlayGrid.Visibility = Visibility.Collapsed;
}
