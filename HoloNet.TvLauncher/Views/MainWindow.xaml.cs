using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Models;
using HoloNet.TvLauncher.Services;
using Microsoft.Extensions.Options;

namespace HoloNet.TvLauncher.Views;

public partial class MainWindow : Window
{
    // Matches CardStyle Width (220) + left/right Margin (12 each) in MainWindow.xaml.
    private const double CardFootprintWidth = 220 + 24;

    // How often the screensaver logo's position is updated — a smooth-looking "DVD logo" drift
    // without needing a full WPF storyboard/animation for something this simple.
    private static readonly TimeSpan ScreensaverAnimationInterval = TimeSpan.FromMilliseconds(30);
    private const double ScreensaverLogoSpeedPixelsPerSecond = 40;

    private readonly IGamesApiClient _gamesApiClient;
    private readonly IGameLauncher _gameLauncher;
    private readonly IGamepadService _gamepadService;
    private readonly ISaveStatsService _saveStatsService;
    private readonly TvLauncherOptions _options;

    private readonly List<GameCardViewModel> _cards = [];
    private int _selectedIndex;
    private bool _isBusy;

    private DateTime _lastActivityUtc = DateTime.UtcNow;
    private readonly DispatcherTimer _idleCheckTimer;
    private DispatcherTimer? _screensaverAnimationTimer;
    private bool _screensaverActive;
    private double _logoX;
    private double _logoY;
    private double _logoVelocityX = ScreensaverLogoSpeedPixelsPerSecond;
    private double _logoVelocityY = ScreensaverLogoSpeedPixelsPerSecond;

    public MainWindow(
        IGamesApiClient gamesApiClient,
        IGameLauncher gameLauncher,
        IGamepadService gamepadService,
        ISaveStatsService saveStatsService,
        IOptions<TvLauncherOptions> options)
    {
        InitializeComponent();

        _gamesApiClient = gamesApiClient;
        _gameLauncher = gameLauncher;
        _gamepadService = gamepadService;
        _saveStatsService = saveStatsService;
        _options = options.Value;

        _idleCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _idleCheckTimer.Tick += (_, _) => CheckIdleTimeout();

        _gamepadService.ButtonPressed += OnGamepadButtonPressed;
        Loaded += async (_, _) =>
        {
            _gamepadService.AttachWindowHandle(new WindowInteropHelper(this).Handle);
            await LoadGamesAsync();
            _gamepadService.Start();
            if (_options.ScreensaverEnabled)
                _idleCheckTimer.Start();
        };
        Closed += (_, _) =>
        {
            _gamepadService.Stop();
            _gamepadService.ButtonPressed -= OnGamepadButtonPressed;
            _idleCheckTimer.Stop();
            _screensaverAnimationTimer?.Stop();
        };
    }

    private async Task LoadGamesAsync()
    {
        SetStatus("Loading game library…");

        try
        {
            var games = await _gamesApiClient.GetGamesAsync();
            _cards.Clear();
            _cards.AddRange(games.Select(g => new GameCardViewModel(g, _saveStatsService.GetStats(g.Title))));
            GamesItemsControl.ItemsSource = _cards;

            _selectedIndex = _cards.Count > 0 ? 0 : -1;
            UpdateSelection();
            SetStatus(_cards.Count == 0
                ? "No games found. Press Start to refresh."
                : "D-pad/stick: move · A: play · Start: refresh · Hold Back+Start while playing: quit");
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

        UpdateSaveInfo();
    }

    private void UpdateSaveInfo()
    {
        var selected = _selectedIndex >= 0 && _selectedIndex < _cards.Count ? _cards[_selectedIndex] : null;
        if (selected is not null && selected.HasSaveStats)
        {
            SaveInfoText.Text = $"{selected.Title}\n{selected.SaveStatsText}";
            SaveInfoText.Visibility = Visibility.Visible;
        }
        else
        {
            SaveInfoText.Visibility = Visibility.Collapsed;
        }
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
            Key.Q => GamepadButton.Quit,
            _ => (GamepadButton?)null
        };

        if (button is not null)
            HandleButton(button.Value);
    }

    private async void HandleButton(GamepadButton button)
    {
        _lastActivityUtc = DateTime.UtcNow;

        if (_screensaverActive)
        {
            HideScreensaver();
            return;
        }

        if (_isBusy)
        {
            if (button is GamepadButton.Confirm or GamepadButton.Cancel && _dismissWait is not null)
                _dismissWait.TrySetResult();

            if (button == GamepadButton.Quit)
                await _gameLauncher.QuitCurrentGameAsync();

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

            // The save file is only updated once the emulator process has actually exited, so
            // refresh this card's stats now (rather than the stale copy fetched at library load)
            // and refresh the on-screen panel if this card is still the selected one.
            _cards[_selectedIndex].SaveStats = _saveStatsService.GetStats(game.Title);
            UpdateSaveInfo();
        }
        finally
        {
            HideOverlay();
            _isBusy = false;
            _lastActivityUtc = DateTime.UtcNow;
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

    private void CheckIdleTimeout()
    {
        // Don't show the screensaver over an emulator (it's covering the window anyway and
        // constantly redrawing, so there's no burn-in risk from TvLauncher itself) or while a
        // modal overlay/error message is up.
        if (_screensaverActive || _isBusy)
            return;

        var idleFor = DateTime.UtcNow - _lastActivityUtc;
        if (idleFor.TotalMinutes >= _options.ScreensaverIdleMinutes)
            ShowScreensaver();
    }

    private void ShowScreensaver()
    {
        _screensaverActive = true;
        ScreensaverGrid.Visibility = Visibility.Visible;

        _logoX = 0;
        _logoY = 0;
        _logoVelocityX = ScreensaverLogoSpeedPixelsPerSecond;
        _logoVelocityY = ScreensaverLogoSpeedPixelsPerSecond;

        _screensaverAnimationTimer ??= new DispatcherTimer { Interval = ScreensaverAnimationInterval };
        _screensaverAnimationTimer.Tick -= OnScreensaverAnimationTick;
        _screensaverAnimationTimer.Tick += OnScreensaverAnimationTick;
        _screensaverAnimationTimer.Start();
    }

    private void HideScreensaver()
    {
        _screensaverActive = false;
        ScreensaverGrid.Visibility = Visibility.Collapsed;
        _screensaverAnimationTimer?.Stop();
    }

    private void OnScreensaverAnimationTick(object? sender, EventArgs e)
    {
        var maxX = Math.Max(0, ActualWidth - ScreensaverLogo.ActualWidth);
        var maxY = Math.Max(0, ActualHeight - ScreensaverLogo.ActualHeight);
        var deltaSeconds = ScreensaverAnimationInterval.TotalSeconds;

        _logoX += _logoVelocityX * deltaSeconds;
        _logoY += _logoVelocityY * deltaSeconds;

        if (_logoX <= 0 || _logoX >= maxX)
        {
            _logoX = Math.Clamp(_logoX, 0, maxX);
            _logoVelocityX = -_logoVelocityX;
        }

        if (_logoY <= 0 || _logoY >= maxY)
        {
            _logoY = Math.Clamp(_logoY, 0, maxY);
            _logoVelocityY = -_logoVelocityY;
        }

        ScreensaverLogoTransform.X = _logoX;
        ScreensaverLogoTransform.Y = _logoY;
    }
}
