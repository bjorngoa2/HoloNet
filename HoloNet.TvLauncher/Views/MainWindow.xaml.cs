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

    private readonly IGamesApiClient _gamesApiClient;
    private readonly IGameLauncher _gameLauncher;
    private readonly IGamepadService _gamepadService;
    private readonly ISaveStatsService _saveStatsService;
    private readonly IGameScreenshotService _screenshotService;
    private readonly IAppUpdateService _appUpdateService;
    private readonly TvLauncherOptions _options;

    private readonly FolderNavigator _navigator;
    private readonly ScreensaverController _screensaver;
    private int _selectedIndex;
    private bool _isBusy;
    private AppUpdateInfo? _pendingUpdate;
    private bool _updateDismissedThisSession;

    /// <summary>
    /// Dispatches "the player selected this card" to the right behavior for its concrete type
    /// (see <see cref="IPickerCardVisitor{TResult}"/>) — folder tiles navigate, shortcut and game
    /// tiles launch — without <see cref="LaunchSelectedAsync"/> needing to type-check/downcast
    /// the selected card itself.
    /// </summary>
    private sealed class SelectionVisitor(MainWindow window) : IPickerCardVisitor<Task>
    {
        public Task VisitFolder(FolderCardViewModel folder)
        {
            window.EnterFolder(folder);
            return Task.CompletedTask;
        }

        public Task VisitShortcut(ShortcutCardViewModel shortcut) => window.LaunchShortcutAsync(shortcut);

        public Task VisitGame(GameCardViewModel game) => window.LaunchGameAsync(game);
    }

    private readonly SelectionVisitor _selectionVisitor;

    public MainWindow(
        IGamesApiClient gamesApiClient,
        IGameLauncher gameLauncher,
        IGamepadService gamepadService,
        ISaveStatsService saveStatsService,
        IGameScreenshotService screenshotService,
        IAppUpdateService appUpdateService,
        IOptions<TvLauncherOptions> options)
    {
        InitializeComponent();

        _selectionVisitor = new SelectionVisitor(this);

        _gamesApiClient = gamesApiClient;
        _gameLauncher = gameLauncher;
        _gamepadService = gamepadService;
        _saveStatsService = saveStatsService;
        _screenshotService = screenshotService;
        _appUpdateService = appUpdateService;
        _options = options.Value;

        _navigator = new FolderNavigator(_options);
        _screensaver = new ScreensaverController(
            _options,
            ScreensaverGrid,
            ScreensaverLogo,
            ScreensaverLogoTransform,
            () => ActualWidth,
            () => ActualHeight,
            () => _isBusy);

        _gamepadService.ButtonPressed += OnGamepadButtonPressed;
        _gamepadService.ControllerKindChanged += OnControllerKindChanged;
        VersionText.Text = $"v{FormatDisplayVersion(_appUpdateService.CurrentVersion)}";
        Loaded += async (_, _) =>
        {
            var windowHandle = new WindowInteropHelper(this).Handle;
            _gamepadService.AttachWindowHandle(windowHandle);
            await RefreshAsync();
            _gamepadService.Start();
            _screensaver.Start();
            _ = CheckForUpdateInBackgroundAsync();
        };
        Closed += (_, _) =>
        {
            _gamepadService.Stop();
            _gamepadService.ButtonPressed -= OnGamepadButtonPressed;
            _gamepadService.ControllerKindChanged -= OnControllerKindChanged;
            _screensaver.Dispose();
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
            _navigator.SetGames(games.Select(g =>
            {
                var card = new GameCardViewModel(g, _saveStatsService.GetStats(g.Title));
                if (_options.ShowcaseScreenshotEnabled)
                    card.RefreshScreenshot(_screenshotService);
                return card;
            }));

            ApplyCards();
        }
        catch (Exception ex)
        {
            SetStatus($"Could not reach the Games API: {ex.Message}. Press {RefreshButtonLabel} to retry.");
        }
    }

    private void EnterFolder(FolderCardViewModel folder)
    {
        _navigator.EnterFolder(folder, _selectedIndex);
        ApplyCards();
    }

    /// <summary>
    /// Checks GitHub Releases for a newer version in the background (see
    /// <see cref="IAppUpdateService"/>) and, if one downloads successfully, folds a "🔔 Update
    /// ready" hint into the existing status line rather than showing any new persistent screen
    /// element (this picker can sit on-screen for days, so a fixed badge would itself become an
    /// OLED burn-in risk). Pressing Start while the hint is showing opens the confirm modal (see
    /// <see cref="ShowUpdateModalAsync"/>); never surfaces anything while busy or mid-game.
    /// </summary>
    private async Task CheckForUpdateInBackgroundAsync()
    {
        var update = await _appUpdateService.CheckAndDownloadUpdateAsync();
        if (update is null)
            return;

        await Dispatcher.InvokeAsync(() =>
        {
            _pendingUpdate = update;
            UpdateHeaderAndStatus();
        });
    }

    /// <summary>
    /// Shows the update confirm modal (reusing <see cref="OverlayGrid"/>, the same surface used
    /// for launch/error states) with an explicit Install/Later choice, and applies the update
    /// only if the player picks Install — "Later" is remembered for the rest of this session
    /// (the status-line hint stays, but Start won't re-open the modal until next app launch).
    /// </summary>
    private async Task ShowUpdateModalAsync(AppUpdateInfo update)
    {
        _isBusy = true;
        ShowOverlay($"Update v{update.NewVersion} is ready to install.\n\n{ConfirmButtonLabel}: Install & restart now    ·    {CancelButtonLabel}: Later");

        _updateModalWait = new TaskCompletionSource<bool>();
        var installNow = await _updateModalWait.Task;
        _updateModalWait = null;

        HideOverlay();
        _isBusy = false;

        if (installNow)
        {
            _appUpdateService.ApplyUpdateAndRestart(update);
        }
        else
        {
            _updateDismissedThisSession = true;
            UpdateHeaderAndStatus();
        }
    }

    private void GoBack()
    {
        if (!_navigator.CanGoBack)
            return;

        _selectedIndex = _navigator.GoBack();
        SetItemsSource();
        UpdateSelection();
        UpdateHeaderAndStatus();
    }

    /// <summary>
    /// Pushes <see cref="FolderNavigator.Cards"/> to the UI and refreshes selection/header/status
    /// text — the common tail end of showing any screen (root, entering a folder, or a folder
    /// refresh).
    /// </summary>
    private void ApplyCards()
    {
        SetItemsSource();
        _selectedIndex = _navigator.Cards.Count > 0 ? 0 : -1;
        UpdateSelection();
        UpdateHeaderAndStatus();
    }

    /// <summary>
    /// Re-points <see cref="GamesItemsControl"/> at <see cref="FolderNavigator.Cards"/>.
    /// <c>Cards</c> is one mutable list reused for every screen (root, a folder's contents,
    /// going back) rather than a fresh list per screen — but WPF's <c>ItemsSource</c> only
    /// regenerates the grid when the assigned reference actually changes, so reassigning the
    /// same reference after mutating it is silently ignored. Clearing to <c>null</c> first
    /// forces a genuine reference change on every call so the grid always picks up the new
    /// contents.
    /// </summary>
    private void SetItemsSource()
    {
        GamesItemsControl.ItemsSource = null;
        GamesItemsControl.ItemsSource = _navigator.Cards;
    }

    private void UpdateHeaderAndStatus()
    {
        HeaderText.Text = $"HoloNet — {_navigator.HeaderTitle}";

        var backHint = _navigator.CanGoBack ? $" · {CancelButtonLabel}: back" : "";
        var baseStatus = _navigator.Cards.Count == 0
            ? $"Nothing here. Press {RefreshButtonLabel} to refresh.{backHint}"
            : $"D-pad/stick: move · {ConfirmButtonLabel}: select · {RefreshButtonLabel}: refresh · Hold {QuitComboLabel} while playing: quit{backHint}";

        var missingEmulatorPlatforms = _gameLauncher.GetMissingEmulatorPlatforms();
        if (missingEmulatorPlatforms.Count > 0)
            baseStatus = $"⚠ Not installed: {string.Join(", ", missingEmulatorPlatforms)} — those games won't launch. · {baseStatus}";

        SetStatus(_pendingUpdate is not null && !_updateDismissedThisSession
            ? $"🔔 Update v{FormatDisplayVersion(_pendingUpdate.NewVersion)} ready — {RefreshButtonLabel}: view · {baseStatus}"
            : baseStatus);
    }

    /// <summary>
    /// Strips Velopack's build-metadata suffix (e.g. the "+a1b2c3d" commit hash SemVer allows
    /// after a "+") from a version string for display — that detail is useful in logs/CI but is
    /// just visual noise for a player glancing at a small on-screen version label.
    /// </summary>
    private static string FormatDisplayVersion(string version)
    {
        var plusIndex = version.IndexOf('+');
        return plusIndex < 0 ? version : version[..plusIndex];
    }

    private void SetStatus(string message) => StatusText.Text = message;

    private void UpdateSelection()
    {
        for (var i = 0; i < _navigator.Cards.Count; i++)
            _navigator.Cards[i].IsSelected = i == _selectedIndex;

        UpdateSaveInfo();
    }

    private void UpdateSaveInfo()
    {
        var selected = _selectedIndex >= 0 && _selectedIndex < _navigator.Cards.Count
            ? _navigator.Cards[_selectedIndex] as GameCardViewModel
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

    /// <summary>
    /// Refreshes button-prompt text (status line, overlays) whenever the active controller
    /// switches families (e.g. an Xbox pad connects after a DualSense was active) — see
    /// <see cref="ConfirmButtonLabel"/> etc., which read <see cref="IGamepadService.CurrentControllerKind"/>
    /// live, so this handler just needs to re-render whatever's currently on screen.
    /// </summary>
    private void OnControllerKindChanged(object? sender, GamepadKind kind) =>
        Dispatcher.Invoke(UpdateHeaderAndStatus);

    private string ConfirmButtonLabel => _gamepadService.CurrentControllerKind == GamepadKind.Xbox ? "A" : "Cross";
    private string CancelButtonLabel => _gamepadService.CurrentControllerKind == GamepadKind.Xbox ? "B" : "Circle";
    private string RefreshButtonLabel => _gamepadService.CurrentControllerKind == GamepadKind.Xbox ? "Start" : "Options";
    private string QuitComboLabel => _gamepadService.CurrentControllerKind == GamepadKind.Xbox ? "Back+Start" : "Share+Options";

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
        _screensaver.NotifyActivity();

        if (_screensaver.IsActive)
        {
            _screensaver.Dismiss();
            return;
        }

        if (_isBusy)
        {
            if (_updateModalWait is not null && button is GamepadButton.Confirm or GamepadButton.Cancel)
            {
                _updateModalWait.TrySetResult(button == GamepadButton.Confirm);
                return;
            }

            if (button is GamepadButton.Confirm or GamepadButton.Cancel && _dismissWait is not null)
                _dismissWait.TrySetResult();

            if (button == GamepadButton.Quit)
            {
                await _gameLauncher.QuitCurrentGameAsync();
            }

            return;
        }

        if (button == GamepadButton.Refresh && _pendingUpdate is not null && !_updateDismissedThisSession)
        {
            await ShowUpdateModalAsync(_pendingUpdate);
            return;
        }

        if (_navigator.Cards.Count == 0)
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
        if (next < 0 || next >= _navigator.Cards.Count)
            return;

        _selectedIndex = next;
        UpdateSelection();
    }

    private Task LaunchSelectedAsync()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _navigator.Cards.Count)
            return Task.CompletedTask;

        return _navigator.Cards[_selectedIndex].Accept(_selectionVisitor);
    }

    private async Task LaunchShortcutAsync(ShortcutCardViewModel shortcut)
    {
        if (!_gameLauncher.LaunchShortcut(shortcut.Shortcut.Url))
        {
            ShowOverlay($"Couldn't open \"{shortcut.Title}\".\n\nPress {ConfirmButtonLabel} to dismiss.");
            await WaitForDismissAsync();
            HideOverlay();
        }
    }

    private async Task LaunchGameAsync(GameCardViewModel gameCard)
    {
        var game = gameCard.Game;

        _isBusy = true;
        ShowOverlay($"Launching {game.Title}…");

        try
        {
            var launchIntent = await _gamesApiClient.GetLaunchIntentAsync(game.Id);
            if (launchIntent is null)
            {
                ShowOverlay($"\"{game.Title}\" has no network path configured and can't be launched.\n\nPress {ConfirmButtonLabel} to dismiss.");
                await WaitForDismissAsync();
                return;
            }

            var result = await StartShowcaseTimerAndLaunchAsync(launchIntent);
            _gamepadService.ForceDirectInputReacquire();
            if (result.Outcome != LaunchOutcome.Success)
            {
                ShowOverlay($"Couldn't launch \"{game.Title}\":\n{result.ErrorMessage}\n\nPress {ConfirmButtonLabel} to dismiss.");
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
            _screensaver.NotifyActivity();
        }
    }

    private TaskCompletionSource? _dismissWait;
    private TaskCompletionSource<bool>? _updateModalWait;

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
}
