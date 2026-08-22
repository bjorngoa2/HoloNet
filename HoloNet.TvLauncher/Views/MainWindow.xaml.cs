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
    private readonly IGameScreenshotService _screenshotService;
    private readonly TvLauncherOptions _options;

    private readonly List<IPickerCard> _cards = [];
    private readonly List<GameCardViewModel> _allGameCards = [];
    private readonly Stack<NavFrame> _navigationStack = new();
    private FolderCardViewModel? _currentFolder;
    private string _headerTitle = "Home";
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

    /// <summary>
    /// Snapshot of a screen pushed onto <see cref="_navigationStack"/> when descending into a
    /// folder (see <see cref="EnterFolder"/>), so <see cref="GoBack"/> can restore exactly what
    /// was on screen (including the previously-selected card) rather than rebuilding it.
    /// </summary>
    private sealed record NavFrame(string HeaderTitle, List<IPickerCard> Cards, int SelectedIndex, FolderCardViewModel? Folder);

    public MainWindow(
        IGamesApiClient gamesApiClient,
        IGameLauncher gameLauncher,
        IGamepadService gamepadService,
        ISaveStatsService saveStatsService,
        IGameScreenshotService screenshotService,
        IOptions<TvLauncherOptions> options)
    {
        InitializeComponent();

        _gamesApiClient = gamesApiClient;
        _gameLauncher = gameLauncher;
        _gamepadService = gamepadService;
        _saveStatsService = saveStatsService;
        _screenshotService = screenshotService;
        _options = options.Value;

        _idleCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _idleCheckTimer.Tick += (_, _) => CheckIdleTimeout();

        _gamepadService.ButtonPressed += OnGamepadButtonPressed;
        Loaded += async (_, _) =>
        {
            _gamepadService.AttachWindowHandle(new WindowInteropHelper(this).Handle);
            await RefreshAsync();
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

    /// <summary>
    /// (Re)fetches the game library and rebuilds whichever screen is currently on display: the
    /// root screen (Games folder + shortcuts) if not inside a folder, or just the current
    /// folder's contents if browsing one (see <see cref="EnterFolder"/>) — refreshing never
    /// bounces the player back out to the root screen.
    /// </summary>
    private async Task RefreshAsync()
    {
        SetStatus("Loading…");

        try
        {
            var games = await _gamesApiClient.GetGamesAsync();
            _allGameCards.Clear();
            _allGameCards.AddRange(games.Select(g =>
            {
                var card = new GameCardViewModel(g, _saveStatsService.GetStats(g.Title));
                if (_options.ShowcaseScreenshotEnabled)
                    card.RefreshScreenshot(_screenshotService);
                return card;
            }));

            if (_currentFolder is null)
            {
                _navigationStack.Clear();
                ShowRootScreen();
            }
            else
            {
                // The current folder's children factory reads live from _allGameCards, so
                // re-invoking it after the fetch above already reflects the fresh data — no
                // need to rebuild the whole navigation path from scratch.
                _cards.Clear();
                _cards.AddRange(_currentFolder.GetChildren());
                ApplyCards();
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Could not reach the Games API: {ex.Message}. Press Start to retry.");
        }
    }

    private void ShowRootScreen()
    {
        _currentFolder = null;
        _headerTitle = "Home";

        var gamesFolder = new FolderCardViewModel(
            "Games",
            $"{_allGameCards.Count} game{(_allGameCards.Count == 1 ? "" : "s")}",
            BuildPlatformFolders);

        _cards.Clear();
        _cards.Add(gamesFolder);
        _cards.AddRange(_options.Shortcuts.Select(s => (IPickerCard)new ShortcutCardViewModel(s)));

        ApplyCards();
    }

    private List<IPickerCard> BuildPlatformFolders() =>
        _allGameCards
            .GroupBy(c => c.Platform, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => DisplayNameForPlatform(g.Key), StringComparer.OrdinalIgnoreCase)
            .Select(g => (IPickerCard)new FolderCardViewModel(
                DisplayNameForPlatform(g.Key),
                $"{g.Count()} game{(g.Count() == 1 ? "" : "s")}",
                () => g.Cast<IPickerCard>().ToList()))
            .ToList();

    private string DisplayNameForPlatform(string platform) =>
        _options.PlatformDisplayNames.TryGetValue(platform, out var displayName) ? displayName : platform;

    private void EnterFolder(FolderCardViewModel folder)
    {
        _navigationStack.Push(new NavFrame(_headerTitle, [.. _cards], _selectedIndex, _currentFolder));

        _currentFolder = folder;
        _headerTitle = folder.Title;
        _cards.Clear();
        _cards.AddRange(folder.GetChildren());

        ApplyCards();
    }

    private void GoBack()
    {
        if (_navigationStack.Count == 0)
            return;

        var frame = _navigationStack.Pop();
        _currentFolder = frame.Folder;
        _headerTitle = frame.HeaderTitle;
        _cards.Clear();
        _cards.AddRange(frame.Cards);

        SetItemsSource();
        _selectedIndex = _cards.Count > 0 ? Math.Clamp(frame.SelectedIndex, 0, _cards.Count - 1) : -1;
        UpdateSelection();
        UpdateHeaderAndStatus();
    }

    /// <summary>
    /// Pushes <see cref="_cards"/> to the UI and refreshes selection/header/status text — the
    /// common tail end of showing any screen (root, entering a folder, or a folder refresh).
    /// </summary>
    private void ApplyCards()
    {
        SetItemsSource();
        _selectedIndex = _cards.Count > 0 ? 0 : -1;
        UpdateSelection();
        UpdateHeaderAndStatus();
    }

    /// <summary>
    /// Re-points <see cref="GamesItemsControl"/> at <see cref="_cards"/>. <c>_cards</c> is one
    /// mutable list reused for every screen (root, a folder's contents, going back) rather than
    /// a fresh list per screen — but WPF's <c>ItemsSource</c> only regenerates the grid when the
    /// assigned reference actually changes, so reassigning the same reference after mutating it
    /// is silently ignored. Clearing to <c>null</c> first forces a genuine reference change on
    /// every call so the grid always picks up the new contents.
    /// </summary>
    private void SetItemsSource()
    {
        GamesItemsControl.ItemsSource = null;
        GamesItemsControl.ItemsSource = _cards;
    }

    private void UpdateHeaderAndStatus()
    {
        HeaderText.Text = $"HoloNet — {_headerTitle}";

        var backHint = _navigationStack.Count > 0 ? " · B: back" : "";
        SetStatus(_cards.Count == 0
            ? $"Nothing here. Press Start to refresh.{backHint}"
            : $"D-pad/stick: move · A: select · Start: refresh · Hold Back+Start while playing: quit{backHint}");
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
        var selected = _selectedIndex >= 0 && _selectedIndex < _cards.Count
            ? _cards[_selectedIndex] as GameCardViewModel
            : null;
        if (selected is not null && selected.HasSaveStats)
        {
            SaveInfoText.Text = $"{selected.Title}\n{selected.SaveStatsText}";
            SaveInfoText.Visibility = Visibility.Visible;
        }
        else
        {
            SaveInfoText.Visibility = Visibility.Collapsed;
        }

        if (selected?.ShowcaseScreenshotPath is { } screenshotPath)
        {
            // WPF caches decoded BitmapImages by URI internally — IgnoreImageCache is required
            // so re-loading the same path after a fresh capture (same file, new content)
            // actually reads the new file instead of returning the previous, stale image.
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.CreateOptions = System.Windows.Media.Imaging.BitmapCreateOptions.IgnoreImageCache;
            bitmap.UriSource = new Uri(screenshotPath, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();

            ShowcaseImage.Source = bitmap;
            ShowcaseBorder.Visibility = Visibility.Visible;
        }
        else
        {
            ShowcaseImage.Source = null;
            ShowcaseBorder.Visibility = Visibility.Collapsed;
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
            {
                await _gameLauncher.QuitCurrentGameAsync();
            }

            return;
        }

        if (_cards.Count == 0)
        {
            if (button == GamepadButton.Refresh)
                await RefreshAsync();
            else if (button == GamepadButton.Cancel)
                GoBack();
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
                await RefreshAsync();
                break;
            case GamepadButton.Cancel:
                GoBack();
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

        if (_cards[_selectedIndex] is FolderCardViewModel folder)
        {
            EnterFolder(folder);
            return;
        }

        if (_cards[_selectedIndex] is ShortcutCardViewModel shortcut)
        {
            if (!_gameLauncher.LaunchShortcut(shortcut.Shortcut.Url))
            {
                ShowOverlay($"Couldn't open \"{shortcut.Title}\".\n\nPress A to dismiss.");
                await WaitForDismissAsync();
                HideOverlay();
            }
            return;
        }

        if (_cards[_selectedIndex] is not GameCardViewModel gameCard)
            return;

        var game = gameCard.Game;

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

            var result = await StartShowcaseTimerAndLaunchAsync(launchIntent);
            if (result.Outcome != LaunchOutcome.Success)
            {
                ShowOverlay($"Couldn't launch \"{game.Title}\":\n{result.ErrorMessage}\n\nPress A to dismiss.");
                await WaitForDismissAsync();
            }

            // The save file is only updated once the emulator process has actually exited, so
            // refresh this card's stats now (rather than the stale copy fetched at library load)
            // and refresh the on-screen panel if this card is still the selected one.
            gameCard.SaveStats = _saveStatsService.GetStats(game.Title);
            if (_options.ShowcaseScreenshotEnabled)
                gameCard.RefreshScreenshot(_screenshotService);
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

    /// <summary>
    /// Runs <see cref="IGameLauncher.LaunchAsync"/> while periodically capturing a "where I
    /// currently am" showcase screenshot (see <see cref="TvLauncherOptions.ShowcaseScreenshotIntervalMinutes"/>)
    /// for as long as the emulator is running, rather than only at quit time. The quit hold-combo
    /// shares its Start button with several emulators' own pause/menu overlay, so capturing then
    /// would often show that menu instead of actual gameplay — a timer avoids that entirely.
    /// Opt-in via <see cref="TvLauncherOptions.ShowcaseScreenshotEnabled"/>; when disabled, this
    /// just launches the game without starting any capture timer.
    /// </summary>
    private async Task<GameLaunchResult> StartShowcaseTimerAndLaunchAsync(LaunchIntentDto launchIntent)
    {
        if (!_options.ShowcaseScreenshotEnabled)
            return await _gameLauncher.LaunchAsync(launchIntent);

        var captureInProgress = 0;
        var timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMinutes(_options.ShowcaseScreenshotIntervalMinutes)
        };
        timer.Tick += (_, _) =>
        {
            if (_gameLauncher.CurrentGameTitle is { } runningTitle)
            {
                // Skip this tick if a previous capture is still running (e.g. the WGC/PCSX2
                // fallback chain took longer than the interval) — letting captures overlap means
                // an older, slower capture can finish AFTER a newer one and overwrite it with a
                // stale image, making the showcase look frozen at an early moment.
                if (Interlocked.CompareExchange(ref captureInProgress, 1, 0) != 0)
                    return;

                var windowHandle = _gameLauncher.CurrentEmulatorWindowHandle;
                // Capture on a background thread — it blocks for a few seconds waiting for
                // PCSX2 to finish writing its screenshot file, and doing that on the UI
                // dispatcher thread would freeze the picker window for that whole time.
                Task.Run(() => _screenshotService.Capture(runningTitle, windowHandle))
                    .ContinueWith(_ => Interlocked.Exchange(ref captureInProgress, 0), TaskScheduler.Default);
            }
        };
        timer.Start();

        try
        {
            return await _gameLauncher.LaunchAsync(launchIntent);
        }
        finally
        {
            timer.Stop();
        }
    }

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
