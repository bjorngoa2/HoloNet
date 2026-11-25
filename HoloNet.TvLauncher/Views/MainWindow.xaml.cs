using System.Globalization;
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
    // How far the tile grid slides up as it fades in when a new screen opens.
    private const double ScreenEntranceOffset = 24;

    // How long a just-started title has to show up among the system media sessions.
    private static readonly TimeSpan MediaSessionLookupTimeout = TimeSpan.FromSeconds(5);

    private readonly IGamesApiClient _gamesApiClient;
    private readonly IGameLauncher _gameLauncher;
    private readonly IGamepadService _gamepadService;
    private readonly ISaveStatsService _saveStatsService;
    private readonly IGameScreenshotService _screenshotService;
    private readonly IAppUpdateService _appUpdateService;
    private readonly StreamingLibrary _streamingLibrary;
    private readonly IMediaLibraryApiClient _mediaLibraryApiClient;
    private readonly IPrimeAuthLauncher _primeAuthLauncher;
    private readonly IPrimePlayerLauncher _primePlayerLauncher;
    private readonly IKioskBrowserLauncher _kioskBrowserLauncher;
    private readonly IMediaSessionService _mediaSessionService;
    private readonly TvLauncherOptions _options;

    private readonly FolderNavigator _navigator;
    private readonly ScreensaverController _screensaver;
    private readonly AppUpdateTracker _updates;
    private readonly DispatcherTimer _updateCheckTimer;
    private readonly CursorAutoHider _cursorHider;
    private readonly DispatcherTimer _clockTimer;
    private int _selectedIndex;
    private bool _isBusy;
    private bool _isLoading;
    private CancellationTokenSource? _refreshCancellation;
    private string? _gamesApiError;
    private HwndSource? _hwndSource;
    private TaskCompletionSource? _dismissWait;
    private TaskCompletionSource<bool>? _confirmWait;
    private PlaybackOverlayController? _playbackOverlay;

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

        public Task VisitMedia(MediaCardViewModel media) => window.LaunchMediaAsync(media);

        public Task VisitPrimeAuthAction(PrimeAuthActionCardViewModel primeAuthAction) => window.RunPrimeAuthAsync(primeAuthAction);

        public Task VisitAddTitleAction(AddTitleActionCardViewModel addTitleAction) => window.AddTitleAsync(addTitleAction);

        public Task VisitSettingChoice(SettingChoiceCardViewModel settingChoice) => window.ChooseSettingAsync(settingChoice);

        public Task VisitCheckForUpdates(CheckForUpdatesActionCardViewModel checkForUpdates) => window.CheckForUpdatesAsync(checkForUpdates);
    }

    private readonly SelectionVisitor _selectionVisitor;

    public MainWindow(
        IGamesApiClient gamesApiClient,
        IGameLauncher gameLauncher,
        IGamepadService gamepadService,
        ISaveStatsService saveStatsService,
        IGameScreenshotService screenshotService,
        IAppUpdateService appUpdateService,
        StreamingLibrary streamingLibrary,
        IMediaLibraryApiClient mediaLibraryApiClient,
        IPrimeAuthLauncher primeAuthLauncher,
        IPrimePlayerLauncher primePlayerLauncher,
        IKioskBrowserLauncher kioskBrowserLauncher,
        IMediaSessionService mediaSessionService,
        ILauncherSettingsStore settingsStore,
        IEnumerable<IScreensaverScene> screensaverScenes,
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
        _streamingLibrary = streamingLibrary;
        _mediaLibraryApiClient = mediaLibraryApiClient;
        _primeAuthLauncher = primeAuthLauncher;
        _primePlayerLauncher = primePlayerLauncher;
        _kioskBrowserLauncher = kioskBrowserLauncher;
        _mediaSessionService = mediaSessionService;
        _options = options.Value;

        _cursorHider = new CursorAutoHider(this);
        _screensaver = new ScreensaverController(_options, settingsStore, ScreensaverGrid, screensaverScenes, () => _isBusy);
        _navigator = new FolderNavigator(
            _options,
            settingsStore,
            new SettingsMenu(_options, settingsStore, _screensaver.Show, FormatDisplayVersion(_appUpdateService.CurrentVersion)));

        // Checks run on the UI thread, but hop through the dispatcher anyway so a check that ever
        // finishes elsewhere can't touch the window off-thread.
        _updates = new AppUpdateTracker(_appUpdateService, _options, TimeProvider.System);
        _updates.Changed += () => Dispatcher.InvokeAsync(UpdateChrome);
        // At least 15 minutes apart whatever the setting says, to stay well inside GitHub's
        // rate limit for unauthenticated requests.
        _updateCheckTimer = new DispatcherTimer { Interval = TimeSpan.FromHours(Math.Max(_options.UpdateCheckIntervalHours, 0.25)) };
        _updateCheckTimer.Tick += (_, _) => _ = _updates.CheckInBackgroundAsync();

        // The TV is unattended while the screensaver is up, a good moment to fetch an update so
        // it's ready to offer when the player comes back.
        _screensaver.Shown += () => _ = _updates.CheckInBackgroundAsync(AppUpdateTracker.ScreensaverCheckMinimumAge);

        _clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _clockTimer.Tick += (_, _) => UpdateClock();
        UpdateClock();

        _gamepadService.ButtonPressed += OnGamepadButtonPressed;
        _gamepadService.ControllerKindChanged += OnControllerKindChanged;
        VersionText.Text = $"v{FormatDisplayVersion(_appUpdateService.CurrentVersion)}";
        Loaded += async (_, _) =>
        {
            var windowHandle = new WindowInteropHelper(this).Handle;
            _gamepadService.AttachWindowHandle(windowHandle);
            _hwndSource = HwndSource.FromHwnd(windowHandle);
            _hwndSource?.AddHook(HandleWindowMessage);
            _clockTimer.Start();
            await RefreshAsync();
            _gamepadService.Start();
            _screensaver.Start();
            _ = _updates.CheckInBackgroundAsync();
            _updateCheckTimer.Start();
        };
        Closed += (_, _) =>
        {
            _clockTimer.Stop();
            _updateCheckTimer.Stop();
            _gamepadService.Stop();
            _gamepadService.ButtonPressed -= OnGamepadButtonPressed;
            _gamepadService.ControllerKindChanged -= OnControllerKindChanged;
            _hwndSource?.RemoveHook(HandleWindowMessage);
            _screensaver.Dispose();
            _cursorHider.Dispose();
        };
    }

    private GamepadKind ControllerKind => _gamepadService.CurrentControllerKind;

    private IPickerCard? SelectedCard =>
        _selectedIndex >= 0 && _selectedIndex < _navigator.Cards.Count ? _navigator.Cards[_selectedIndex] : null;

    /// <summary>
    /// Forwards every window message to <see cref="IGamepadService.ProcessWindowMessage"/> so
    /// its Raw Input fallback reader (see <see cref="RawInputGamepadReader"/>) receives
    /// <c>WM_INPUT</c> — needed because a Bluetooth DualSense can end up in a report mode
    /// DirectInput cannot parse (see <see cref="GamepadInputService"/>'s doc comment).
    /// </summary>
    private IntPtr HandleWindowMessage(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        _gamepadService.ProcessWindowMessage(msg, lParam);
        return IntPtr.Zero;
    }

    /// <summary>
    /// (Re)fetches the game library and rebuilds whichever screen is currently on display: the
    /// root screen (Games folder + shortcuts) if not inside a folder, or just the current
    /// folder's contents if browsing one (see <see cref="EnterFolder"/>) — refreshing never
    /// bounces the player back out to the root screen.
    /// </summary>
    /// <remarks>
    /// Starting a refresh cancels any refresh still in flight (e.g. Menu pressed while the startup
    /// refresh is loading), so a slow, older response can never overwrite newer data.
    /// </remarks>
    private async Task RefreshAsync()
    {
        _refreshCancellation?.Cancel();
        using var cancellation = new CancellationTokenSource();
        _refreshCancellation = cancellation;
        var token = cancellation.Token;

        _isLoading = true;
        UpdateChrome();

        try
        {
            await RefreshGamesAsync(token);
            ShowCurrentScreen(selectedIndex: 0);

            foreach (var service in StreamingService.All.Where(service => service.IsEnabled(_options)))
            {
                if (await TryRefreshStreamingAsync(service, token))
                    ShowCurrentScreen(_selectedIndex);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            // Superseded by a newer refresh, which now owns the screen.
        }
        finally
        {
            if (_refreshCancellation == cancellation)
            {
                _refreshCancellation = null;
                _isLoading = false;
                UpdateChrome();
            }
        }
    }

    private async Task RefreshGamesAsync(CancellationToken cancellationToken)
    {
        try
        {
            var games = await _gamesApiClient.GetGamesAsync(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _navigator.SetGames(games.Select(g =>
            {
                var card = new GameCardViewModel(g, _saveStatsService.GetStats(g.Title));
                if (_options.ShowcaseScreenshotEnabled)
                    card.RefreshScreenshot(_screenshotService);
                return card;
            }));
            _gamesApiError = null;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            _gamesApiError = ex.Message;
        }
    }

    /// <summary>
    /// Best-effort and kept separate from the Games fetch and from other services: a stale or
    /// expired Prime session cookie, a changed Disney+ page, or an unreachable HoloNet.Video
    /// shouldn't block the rest of the library from loading. A title that can't be fetched is
    /// left out, and an unreachable library leaves the folder as it was, rather than surfacing
    /// an error.
    /// </summary>
    private async Task<bool> TryRefreshStreamingAsync(StreamingService service, CancellationToken cancellationToken)
    {
        try
        {
            var titles = await _streamingLibrary.LoadAsync(service, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();

            _navigator.SetStreamingItems(service, titles);
            return true;
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private void EnterFolder(FolderCardViewModel folder)
    {
        _navigator.EnterFolder(folder, _selectedIndex);
        ShowCurrentScreen(selectedIndex: 0);
        PlayScreenEntrance();
    }

    private void GoBack()
    {
        if (!_navigator.CanGoBack)
            return;

        ShowCurrentScreen(_navigator.GoBack());
        PlayScreenEntrance();
    }

    /// <summary>
    /// Whether an update found by <see cref="AppUpdateTracker"/> is waiting to be offered. Rather
    /// than interrupting with a dialog, it adds an "Update ready" notice to the top bar and turns
    /// the Menu hint into "Install update"; pressing Menu then opens the confirm prompt (see
    /// <see cref="ShowUpdateModalAsync"/>).
    /// </summary>
    private bool IsUpdatePending => _updates.IsPending;

    /// <summary>
    /// Asks whether to install the downloaded update now, with an explicit Install/Later choice,
    /// and applies it only if the player picks Install. "Later" puts that version off (Menu goes
    /// back to meaning Refresh) until a newer one comes out or the player checks from Settings.
    /// </summary>
    private async Task ShowUpdateModalAsync(AppUpdateInfo update)
    {
        _isBusy = true;
        var installNow = await ConfirmAsync(new SplashContent(
            $"Update v{FormatDisplayVersion(update.NewVersion)} is ready",
            "HoloNet will restart to finish installing.",
            Hints:
            [
                ButtonPrompts.Hint(ControllerKind, PromptButton.Confirm, "Install & restart"),
                ButtonPrompts.Hint(ControllerKind, PromptButton.Cancel, "Later")
            ]));

        if (installNow)
        {
            Splash.Show(new SplashContent("Installing update", "HoloNet will be right back.", IsWorking: true));
            _appUpdateService.ApplyUpdateAndRestart(update);
            return;
        }

        Splash.Hide();
        _isBusy = false;
        _updates.Dismiss();
    }

    /// <summary>
    /// Settings → Check for updates: checks now and offers the update straight away, or says
    /// why there isn't one.
    /// </summary>
    private async Task CheckForUpdatesAsync(CheckForUpdatesActionCardViewModel card)
    {
        _isBusy = true;
        try
        {
            Splash.Show(new SplashContent(card.Title, "Looking for a newer version of HoloNet…", card, IsWorking: true));
            var result = await _updates.CheckNowAsync();

            if (result.Update is { } update)
            {
                await ShowUpdateModalAsync(update);
                return;
            }

            var (title, message) = result.Outcome switch
            {
                AppUpdateCheckOutcome.UpToDate =>
                    ("HoloNet is up to date", $"v{FormatDisplayVersion(_appUpdateService.CurrentVersion)} is the latest version."),
                AppUpdateCheckOutcome.NotInstalled =>
                    ("This copy can't update itself", "Updates only work for HoloNet installed with its Setup program from GitHub."),
                _ =>
                    ("Couldn't check for updates", "GitHub couldn't be reached. Check the internet connection and try again.")
            };
            await ShowErrorAsync(card, title, message);
        }
        finally
        {
            Splash.Hide();
            _isBusy = false;
        }
    }

    /// <summary>
    /// Pushes <see cref="FolderNavigator.Cards"/> to the tile grid, focuses
    /// <paramref name="selectedIndex"/> (clamped to the new contents) and scrolls it into view.
    /// </summary>
    private void ShowCurrentScreen(int selectedIndex)
    {
        SetItemsSource();

        // A refresh can rebuild Home mid-move; the tile being moved keeps the focus.
        selectedIndex = _navigator.MovingIndex ?? selectedIndex;
        _selectedIndex = _navigator.Cards.Count == 0 ? -1 : Math.Clamp(selectedIndex, 0, _navigator.Cards.Count - 1);

        // Generate the tile containers now, so their positions are known for scrolling and
        // spatial navigation before the next input arrives.
        TilesItemsControl.UpdateLayout();
        UpdateSelection();
        RevealSelected(animate: false);
    }

    /// <summary>
    /// Re-points <see cref="TilesItemsControl"/> at <see cref="FolderNavigator.Cards"/>.
    /// <c>Cards</c> is one mutable list reused for every screen (root, a folder's contents,
    /// going back) rather than a fresh list per screen — but WPF's <c>ItemsSource</c> only
    /// regenerates the grid when the assigned reference actually changes, so reassigning the
    /// same reference after mutating it is silently ignored. Clearing to <c>null</c> first
    /// forces a genuine reference change on every call so the grid always picks up the new
    /// contents.
    /// </summary>
    private void SetItemsSource()
    {
        TilesItemsControl.ItemsSource = null;
        TilesItemsControl.ItemsSource = _navigator.Cards;
    }

    /// <summary>Slides and fades the tile grid in, so moving between screens reads as navigation.</summary>
    private void PlayScreenEntrance()
    {
        TilesOffset.BeginAnimation(TranslateTransform.YProperty, Motion.FromTo(ScreenEntranceOffset, 0, Motion.Standard));
        TilesItemsControl.BeginAnimation(OpacityProperty, Motion.FromTo(0, 1, Motion.Standard));
    }

    private void UpdateSelection()
    {
        for (var i = 0; i < _navigator.Cards.Count; i++)
            _navigator.Cards[i].IsSelected = i == _selectedIndex;

        UpdateHero();
        UpdateChrome();
    }

    private void UpdateHero()
    {
        var selected = SelectedCard;
        Backdrop.Show(selected);

        if (selected is not null)
            Hero.Show(selected);
        else if (_gamesApiError is not null && !_navigator.CanGoBack)
            Hero.ShowMessage("Can't reach the game library", $"{_gamesApiError} Press {MenuButtonName} to try again.");
        else
            Hero.ShowMessage("Nothing here yet", $"Press {MenuButtonName} to refresh.");
    }

    /// <summary>Re-renders the top bar (location and notices) and the hint bar.</summary>
    private void UpdateChrome()
    {
        LocationText.Text = _navigator.HeaderTitle;
        UpdateNotice();
        HintsList.ItemsSource = HomeHints.For(
            ControllerKind,
            new HomeHintState(
                SelectedCard,
                CanRestartSelected(),
                _navigator.CanGoBack,
                IsUpdatePending,
                CanRemoveSelected(),
                CanMove: _navigator.CanMove(_selectedIndex),
                IsMoving: _navigator.IsMoving));
    }

    /// <summary>
    /// Shows at most one short status message in the top bar, most important first. Warnings
    /// (amber) explain why something won't work; anything else is informational.
    /// </summary>
    private void UpdateNotice()
    {
        var (notice, isWarning) = CurrentNotice();
        NoticeText.Text = notice;
        NoticeText.Foreground = (Brush)FindResource(isWarning ? "WarningBrush" : "SubTextBrush");
    }

    private (string Text, bool IsWarning) CurrentNotice()
    {
        // An empty screen explains the library error in the hero panel instead (see UpdateHero).
        if (_gamesApiError is not null && _navigator.Cards.Count > 0)
            return ("Game library unavailable", true);

        var missingEmulatorPlatforms = _gameLauncher.GetMissingEmulatorPlatforms();
        if (missingEmulatorPlatforms.Count > 0)
            return ($"Emulator missing: {string.Join(", ", missingEmulatorPlatforms)}", true);

        if (_isLoading)
            return ("Loading…", false);

        return _updates is { IsPending: true, PendingUpdate: { } pendingUpdate }
            ? ($"Update v{FormatDisplayVersion(pendingUpdate.NewVersion)} ready", false)
            : (string.Empty, false);
    }

    private void UpdateClock() =>
        ClockText.Text = DateTime.Now.ToString("t", CultureInfo.CurrentCulture);

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

    private string MenuButtonName => ButtonPrompts.For(ControllerKind, PromptButton.Menu).Symbol;

    private void OnGamepadButtonPressed(object? sender, GamepadButton button) =>
        Dispatcher.Invoke(() => HandleButton(button));

    /// <summary>
    /// Re-renders the button hints whenever the active controller switches families (e.g. an
    /// Xbox pad connects after a DualSense was active), so prompts always match the pad in hand.
    /// </summary>
    private void OnControllerKindChanged(object? sender, GamepadKind kind) =>
        Dispatcher.Invoke(UpdateChrome);

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        // While a text field has focus (see SplashContent.Input), keys type into it; only Enter
        // and Escape answer the dialog.
        if (e.OriginalSource is System.Windows.Controls.TextBox && e.Key is not (Key.Enter or Key.Escape))
            return;

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
            Key.Y => GamepadButton.Secondary,
            Key.Delete => GamepadButton.Tertiary,
            _ => (GamepadButton?)null
        };

        if (button is null)
            return;

        e.Handled = true;
        HandleButton(button.Value);
    }

    private async void HandleButton(GamepadButton button)
    {
        GamepadDebugLog.Log($"[MainWindow] HandleButton({button}) isBusy={_isBusy} screensaverActive={_screensaver.IsActive} cardCount={_navigator.Cards.Count} selectedIndex={_selectedIndex}");

        _screensaver.NotifyActivity();
        _cursorHider.Hide();

        if (_screensaver.IsActive)
        {
            _screensaver.Dismiss();
            return;
        }

        if (_isBusy)
        {
            if (_playbackOverlay is not null)
            {
                _playbackOverlay.HandleButton(button);
                return;
            }

            if (_confirmWait is not null && button is GamepadButton.Confirm or GamepadButton.Cancel)
            {
                _confirmWait.TrySetResult(button == GamepadButton.Confirm);
                return;
            }

            if (button is GamepadButton.Confirm or GamepadButton.Cancel && _dismissWait is not null)
                _dismissWait.TrySetResult();

            if (button == GamepadButton.Quit && !await _gameLauncher.QuitCurrentGameAsync())
            {
                await _kioskBrowserLauncher.QuitAsync();
            }

            return;
        }

        if (_navigator.IsMoving)
        {
            await HandleMoveButtonAsync(button);
            return;
        }

        switch (button)
        {
            case GamepadButton.Left:
                MoveFocus(NavigationDirection.Left);
                break;
            case GamepadButton.Right:
                MoveFocus(NavigationDirection.Right);
                break;
            case GamepadButton.Up:
                MoveFocus(NavigationDirection.Up);
                break;
            case GamepadButton.Down:
                MoveFocus(NavigationDirection.Down);
                break;
            case GamepadButton.Confirm:
                await LaunchSelectedAsync();
                break;
            case GamepadButton.Secondary:
                if (CanRestartSelected() && SelectedCard is MediaCardViewModel restartMedia)
                    await LaunchMediaAsync(restartMedia, restart: true);
                break;
            case GamepadButton.Tertiary when _navigator.CanMove(_selectedIndex):
                _navigator.BeginMove(_selectedIndex);
                UpdateChrome();
                break;
            case GamepadButton.Tertiary:
                await RemoveSelectedTitleAsync();
                break;
            case GamepadButton.Refresh when _updates is { IsPending: true, PendingUpdate: { } pendingUpdate }:
                await ShowUpdateModalAsync(pendingUpdate);
                break;
            case GamepadButton.Refresh:
                await RefreshAsync();
                break;
            case GamepadButton.Cancel:
                GoBack();
                break;
        }
    }

    private bool CanRemoveSelected() =>
        SelectedCard is { } card && _navigator.TryGetLibraryTitle(card, out _, out _);

    /// <summary>
    /// While a home tile is picked up (see <see cref="FolderNavigator.BeginMove"/>), the D-pad
    /// carries it to the neighbouring tile's spot, Confirm (or the button that picked it up) puts
    /// it down and saves, and Cancel puts everything back. Other buttons do nothing until then.
    /// </summary>
    private async Task HandleMoveButtonAsync(GamepadButton button)
    {
        switch (button)
        {
            case GamepadButton.Left:
                MoveSelectedTile(NavigationDirection.Left);
                break;
            case GamepadButton.Right:
                MoveSelectedTile(NavigationDirection.Right);
                break;
            case GamepadButton.Up:
                MoveSelectedTile(NavigationDirection.Up);
                break;
            case GamepadButton.Down:
                MoveSelectedTile(NavigationDirection.Down);
                break;
            case GamepadButton.Confirm or GamepadButton.Tertiary:
                ShowCurrentScreen(await _navigator.CommitMoveAsync());
                break;
            case GamepadButton.Cancel:
                ShowCurrentScreen(_navigator.CancelMove());
                break;
        }
    }

    private void MoveSelectedTile(NavigationDirection direction)
    {
        if (SpatialNavigator.FindNext(GetTileBounds(), _selectedIndex, direction) is not { } target)
            return;

        var moved = _navigator.MoveTo(target);
        if (moved != _selectedIndex)
            ShowCurrentScreen(moved);
    }

    private bool CanRestartSelected() =>
        SelectedCard is MediaCardViewModel media
        && media.Media.EntityType is 0 or 3
        && !string.IsNullOrWhiteSpace(_options.PrimePlayerToolPath);

    /// <summary>
    /// Moves focus to the visually adjacent tile in <paramref name="direction"/> (see
    /// <see cref="SpatialNavigator"/>), based on where the tiles are actually laid out.
    /// </summary>
    private void MoveFocus(NavigationDirection direction)
    {
        var next = SpatialNavigator.FindNext(GetTileBounds(), _selectedIndex, direction);
        GamepadDebugLog.Log($"[MainWindow] MoveFocus({direction}) from {_selectedIndex} to {next?.ToString() ?? "nowhere"}");
        if (next is null)
            return;

        _selectedIndex = next.Value;
        UpdateSelection();
        RevealSelected(animate: true);
    }

    private List<Rect> GetTileBounds() =>
        Enumerable.Range(0, _navigator.Cards.Count)
            .Select(i => TilesItemsControl.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement tile
                ? new Rect(tile.TranslatePoint(default, TilesItemsControl), tile.RenderSize)
                : Rect.Empty)
            .ToList();

    /// <summary>
    /// Scrolls so the focused tile's row sits at the top of the grid area - the focused row stays
    /// in one predictable place, as on Google TV and tvOS, instead of the page jumping only when
    /// focus reaches an edge. See <see cref="TileGridScroll.OffsetFor"/>.
    /// </summary>
    private void RevealSelected(bool animate)
    {
        if (TilesItemsControl.ItemContainerGenerator.ContainerFromIndex(_selectedIndex) is not FrameworkElement tile)
        {
            SmoothScroll.JumpTo(GridScrollViewer, 0);
            return;
        }

        var rowTop = tile.TranslatePoint(default, TilesItemsControl).Y;
        var contentBottom = TilesItemsControl.Margin.Top + GetTilesBottom();
        var offset = TileGridScroll.OffsetFor(rowTop, contentBottom, GridScrollViewer.ViewportHeight);
        if (animate)
            SmoothScroll.AnimateTo(GridScrollViewer, offset);
        else
            SmoothScroll.JumpTo(GridScrollViewer, offset);
    }

    /// <summary>
    /// The bottom edge of the lowest tile's artwork in grid content coordinates - excluding the
    /// spacing margin after it, which isn't visible.
    /// </summary>
    private double GetTilesBottom() =>
        Enumerable.Range(0, _navigator.Cards.Count)
            .Select(i => TilesItemsControl.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement container
                         && VisualTreeHelper.GetChildrenCount(container) > 0
                         && VisualTreeHelper.GetChild(container, 0) is FrameworkElement art
                ? container.TranslatePoint(default, TilesItemsControl).Y + art.Margin.Top + art.ActualHeight
                : 0)
            .DefaultIfEmpty(0)
            .Max();

    /// <summary>
    /// Fades the grid's edges, and pads its end by a viewport's height so even the last row can
    /// scroll to the top (see <see cref="TileGridScroll"/>).
    /// </summary>
    private void OnGridViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        GridScrollViewer.OpacityMask = TileGridScroll.EdgeFadeMask(e.NewSize.Height);
        var margin = TilesItemsControl.Margin;
        TilesItemsControl.Margin = new Thickness(margin.Left, margin.Top, margin.Right, e.NewSize.Height);
    }

    private Task LaunchSelectedAsync() => SelectedCard?.Accept(_selectionVisitor) ?? Task.CompletedTask;

    private async Task LaunchShortcutAsync(ShortcutCardViewModel shortcut)
    {
        if (shortcut.Shortcut.Kiosk)
        {
            await RunKioskShortcutAsync(shortcut);
            return;
        }

        if (!_gameLauncher.LaunchShortcut(shortcut.Shortcut.Url))
            await ShowErrorAsync(shortcut, $"Couldn't open {shortcut.Title}", "The link couldn't be opened.");
    }

    /// <summary>
    /// Runs a kiosk shortcut (see <see cref="ShortcutMapping.Kiosk"/>) as a managed session, same
    /// UX shape as <see cref="LaunchGameAsync"/>: the picker is blocked until the kiosk browser
    /// exits (via the quit combo - see <see cref="IKioskBrowserLauncher.QuitAsync"/>).
    /// </summary>
    private Task RunKioskShortcutAsync(ShortcutCardViewModel shortcut) =>
        RunSessionAsync(shortcut, LaunchSplash(shortcut, "Starting…"), async () =>
        {
            var result = await _kioskBrowserLauncher.RunAsync(shortcut.Shortcut.Url);
            return result.Outcome == KioskBrowserLaunchOutcome.Success
                ? null
                : new SessionFailure($"Couldn't open {shortcut.Title}", result.ErrorMessage);
        });

    /// <summary>
    /// Plays a streaming (Prime Video, Disney+ or HBO Max) movie/episode: tries <see cref="IPrimePlayerLauncher"/> first (which
    /// launches your real Firefox, starts playback, and goes fullscreen on its own - see
    /// <see cref="Services.PrimePlayerLauncher"/>), falling back to just opening the deep link in
    /// the system default browser (same as <see cref="LaunchShortcutAsync"/>) when PrimePlayer
    /// isn't configured, isn't installed, or fails. Only movies/episodes get the auto-play
    /// treatment (entity types 0/3 - see <see cref="MediaCardViewModel"/>'s doc comment for the
    /// numeric mapping); any other leaf type (which shouldn't normally reach here, since series/
    /// seasons become drill-in folders) just falls back to opening the link, same as before.
    ///
    /// <paramref name="restart"/> (triggered via <see cref="GamepadButton.Secondary"/> -
    /// "Y"/"Triangle") forces playback to start from position 0 instead of resuming - see
    /// <see cref="IPrimePlayerLauncher.PlayAsync"/>.
    /// </summary>
    private async Task LaunchMediaAsync(MediaCardViewModel media, bool restart = false)
    {
        var deepLinkUrl = media.Media.DeepLinkUrl;
        if (string.IsNullOrWhiteSpace(deepLinkUrl))
        {
            await ShowErrorAsync(media, $"Couldn't play {media.Title}", "This title has no link to open.");
            return;
        }

        var linkFailure = new SessionFailure($"Couldn't play {media.Title}", "The link couldn't be opened.");
        var isMovieOrEpisode = media.Media.EntityType is 0 or 3;
        if (!isMovieOrEpisode || string.IsNullOrWhiteSpace(_options.PrimePlayerToolPath))
        {
            if (!_gameLauncher.LaunchShortcut(deepLinkUrl))
                await ShowErrorAsync(media, linkFailure.Title, linkFailure.Message);
            return;
        }

        var splash = new SplashContent(
            media.Title,
            restart ? "Starting from the beginning…" : "Starting…",
            media,
            IsWorking: true);

        await RunSessionAsync(media, splash, async () =>
        {
            // Keep the splash above Firefox while PrimePlayer works, so the player sees the title
            // open straight into playback rather than the page being clicked through.
            var curtain = PlaybackCurtainWindow.Cover(this, splash);
            PrimePlayerLaunchResult result;
            try
            {
                result = await _primePlayerLauncher.PlayAsync(deepLinkUrl, media.Title, restart);
            }
            finally
            {
                await curtain.LiftAsync();
            }

            // If the auto-play automation fails, open the link directly so the player still gets
            // to watch something.
            if (result.Outcome != PrimePlayerLaunchOutcome.Success)
                return _gameLauncher.LaunchShortcut(deepLinkUrl) ? null : linkFailure;

            await WatchAsync(media);
            return null;
        });
    }

    /// <summary>
    /// Shows HoloNet's own playback controls over the browser while the title plays (see
    /// <see cref="PlaybackOverlayController"/>), keeping the picker blocked until the viewer
    /// returns home or the browser goes away. Returns straight away if the browser doesn't
    /// publish a media session, leaving the site's own controls in charge.
    /// </summary>
    private async Task WatchAsync(MediaCardViewModel media)
    {
        using var session = await _mediaSessionService.WaitForSessionAsync(_primePlayerLauncher.MediaSourceAppId, MediaSessionLookupTimeout);
        if (session is null)
            return;

        // A reused tab can still be paused from last time.
        if (!session.Current.IsPlaying)
            await session.PlayAsync();

        // Nothing animates behind the video; returning home fades out from the title's art.
        Splash.Show(new SplashContent(media.Title, string.Empty, media));

        using var overlay = new PlaybackOverlayController(
            session,
            PlaybackOverlayWindow.Create(this, media.Title, media.Breadcrumb),
            () => ControllerKind);
        _playbackOverlay = overlay;
        try
        {
            await overlay.RunAsync();
        }
        finally
        {
            _playbackOverlay = null;
        }

        // The browser keeps running (paused) behind the launcher.
        ForegroundWindows.TakeForeground(this);
    }

    /// <summary>
    /// Runs the Prime Video login refresh flow (see <see cref="IPrimeAuthLauncher"/>): launches
    /// the standalone browser-login tool and blocks the picker with a splash until it exits,
    /// same UX shape as launching a game. On success, re-fetches the catalog immediately so a
    /// just-fixed login shows up in the Prime Video folder without waiting for the next manual
    /// refresh.
    /// </summary>
    private async Task RunPrimeAuthAsync(PrimeAuthActionCardViewModel card)
    {
        var splash = new SplashContent(
            card.Title,
            "A browser window will open. Sign in there, then follow its instructions.",
            card,
            IsWorking: true);

        await RunSessionAsync(card, splash, async () =>
        {
            var result = await _primeAuthLauncher.RunLoginFlowAsync();
            if (result.Outcome != PrimeAuthLaunchOutcome.Success)
                return new SessionFailure("Prime Video sign-in didn't finish", result.ErrorMessage);

            Splash.Show(new SplashContent(card.Title, "Signed in. Reloading your catalog…", card, IsWorking: true));
            await RefreshAsync();
            return null;
        });
    }

    /// <summary>
    /// Adds a show or film to a streaming folder: asks for a link (prefilled from the clipboard,
    /// see <see cref="ClipboardLink"/>), adds it to HoloNet.Video's library, and focuses its new
    /// tile. A rejected link is explained on the same dialog so it can be corrected or cancelled.
    /// HoloNet.Video works out the service from the link, so a link to another service's title
    /// lands in that service's folder instead.
    /// </summary>
    private async Task AddTitleAsync(AddTitleActionCardViewModel card)
    {
        _isBusy = true;
        try
        {
            var link = ClipboardLink.Read();
            var message = $"Paste a link to a show or film from the {card.Service.Name} website.";
            var isWarning = false;

            while (true)
            {
                var confirmed = await ConfirmAsync(new SplashContent(
                    card.Title,
                    message,
                    card,
                    Hints:
                    [
                        ButtonPrompts.Hint(ControllerKind, PromptButton.Confirm, "Add"),
                        ButtonPrompts.Hint(ControllerKind, PromptButton.Cancel, "Cancel")
                    ],
                    Input: new SplashInput(link, card.Service.ExampleLink),
                    IsWarning: isWarning));
                if (!confirmed)
                    return;

                link = Splash.InputText.Trim();
                if (link.Length == 0)
                {
                    (message, isWarning) = ("Paste a link first.", true);
                    continue;
                }

                Splash.Show(new SplashContent(card.Title, "Adding…", card, IsWorking: true));
                var result = await _mediaLibraryApiClient.ImportAsync(link);
                if (result.Title is null)
                {
                    (message, isWarning) = (result.ErrorMessage ?? "That link couldn't be added.", true);
                    continue;
                }

                await ShowAddedTitleAsync(card, result.Title);
                return;
            }
        }
        finally
        {
            Splash.Hide();
            _isBusy = false;
            Focus();
        }
    }

    /// <summary>
    /// Applies an option from the Settings folder, then refreshes its sibling tiles so the check
    /// mark moves to the option now in use.
    /// </summary>
    private async Task ChooseSettingAsync(SettingChoiceCardViewModel choice)
    {
        await choice.ChooseAsync();

        foreach (var option in _navigator.Cards.OfType<SettingChoiceCardViewModel>())
            option.Refresh();

        UpdateHero();
    }

    /// <summary>
    /// Puts a just-added title at the front of its service's folder and focuses it, or explains
    /// where it went when that isn't the folder on screen.
    /// </summary>
    private async Task ShowAddedTitleAsync(AddTitleActionCardViewModel card, LibraryTitleDto added)
    {
        var service = StreamingService.ForProvider(added.Provider);
        if (service is null)
        {
            await ShowErrorAsync(card, $"Added {added.Title}", "It's from a streaming service this launcher doesn't show.");
            return;
        }

        var title = await _streamingLibrary.LoadTitleAsync(service, added.ProviderMediaId);
        if (title is null)
        {
            await ShowErrorAsync(card, $"Added {added.Title}", $"It'll appear in {service.Name} once its details can be loaded.");
            return;
        }

        _navigator.SetStreamingItems(
            service,
            [title, .. _navigator.StreamingTitles(service).Where(t => t.LibraryId != title.LibraryId)]);
        ShowCurrentScreen(Math.Max(_navigator.Cards.IndexOf(title.Card), 0));

        if (service != card.Service)
            await ShowErrorAsync(title.Card, $"Added {added.Title}", $"It's a {service.Name} title, so it's in the {service.Name} folder.");
    }

    /// <summary>
    /// Removes the focused title from its streaming folder (and HoloNet.Video's library), after
    /// asking to confirm. Only titles listed directly in a service's folder can be removed (see
    /// <see cref="FolderNavigator.TryGetLibraryTitle"/>).
    /// </summary>
    private async Task RemoveSelectedTitleAsync()
    {
        if (SelectedCard is not { } card || !_navigator.TryGetLibraryTitle(card, out var service, out var title))
            return;

        _isBusy = true;
        try
        {
            var confirmed = await ConfirmAsync(new SplashContent(
                $"Remove {card.Title}?",
                $"It'll no longer be listed in {service.Name}. You can add it again any time.",
                card,
                Hints:
                [
                    ButtonPrompts.Hint(ControllerKind, PromptButton.Confirm, "Remove"),
                    ButtonPrompts.Hint(ControllerKind, PromptButton.Cancel, "Keep")
                ]));
            if (!confirmed)
                return;

            Splash.Show(new SplashContent(card.Title, "Removing…", card, IsWorking: true));
            if (!await _mediaLibraryApiClient.RemoveAsync(service.CatalogProvider, title.LibraryId))
            {
                await ShowErrorAsync(card, $"Couldn't remove {card.Title}", "HoloNet.Video couldn't be reached. Try again in a moment.");
                return;
            }

            var index = _selectedIndex;
            _navigator.SetStreamingItems(service, _navigator.StreamingTitles(service).Where(t => t != title));
            ShowCurrentScreen(index);
        }
        finally
        {
            Splash.Hide();
            _isBusy = false;
        }
    }

    /// <summary>
    /// Shows a dialog on the splash and waits for Confirm (<c>true</c>) or Cancel (<c>false</c>);
    /// see <see cref="HandleButton"/>. The splash stays up, so the caller can follow on from it.
    /// </summary>
    private async Task<bool> ConfirmAsync(SplashContent dialog)
    {
        Splash.Show(dialog);
        _confirmWait = new TaskCompletionSource<bool>();
        try
        {
            return await _confirmWait.Task;
        }
        finally
        {
            _confirmWait = null;
        }
    }
    private async Task LaunchGameAsync(GameCardViewModel gameCard)
    {
        var game = gameCard.Game;

        await RunSessionAsync(gameCard, LaunchSplash(gameCard, $"Starting {game.Platform}…"), async () =>
        {
            var launchIntent = await _gamesApiClient.GetLaunchIntentAsync(game.Id);
            if (launchIntent is null)
                return new SessionFailure($"Couldn't start {game.Title}", "This game has no network path configured.");

            var result = await StartShowcaseTimerAndLaunchAsync(launchIntent);

            // The save file is only updated once the emulator process has actually exited, so
            // refresh this card's stats now (rather than the stale copy fetched at library load)
            // and refresh the hero panel if this card is still the selected one.
            gameCard.SaveStats = _saveStatsService.GetStats(game.Title);
            if (_options.ShowcaseScreenshotEnabled)
                gameCard.RefreshScreenshot(_screenshotService);
            UpdateHero();

            return result.Outcome == LaunchOutcome.Success
                ? null
                : new SessionFailure($"Couldn't start {game.Title}", result.ErrorMessage);
        });
    }

    /// <summary>Why a session couldn't start, shown on the splash until dismissed.</summary>
    private sealed record SessionFailure(string Title, string? Message);

    /// <summary>
    /// Runs a game, app or sign-in session: the picker is blocked behind <paramref name="splash"/>
    /// until <paramref name="session"/> completes, any failure it reports is shown before
    /// returning home, and the gamepad is reacquired, since the session's own window may have
    /// taken exclusive input.
    /// </summary>
    private async Task RunSessionAsync(IPickerCard card, SplashContent splash, Func<Task<SessionFailure?>> session)
    {
        _isBusy = true;
        Splash.Show(splash);

        try
        {
            var failure = await session();
            _gamepadService.ForceDirectInputReacquire();

            if (failure is not null)
                await ShowErrorAsync(card, failure.Title, failure.Message);
        }
        finally
        {
            EndSession();
        }
    }

    /// <summary>
    /// The splash shown while a game or app is starting. It stays up behind the game or app for
    /// the whole session, so returning home fades back from the same screen.
    /// </summary>
    private SplashContent LaunchSplash(IPickerCard card, string message) => new(
        card.Title,
        message,
        card,
        IsWorking: true,
        Hints: [ButtonPrompts.QuitHint(ControllerKind, "Return home")]);

    /// <summary>Returns to the picker after a game, app or sign-in session.</summary>
    private void EndSession()
    {
        Splash.Hide();
        _isBusy = false;
        _screensaver.NotifyActivity();
    }

    /// <summary>
    /// Shows an error on the splash and waits for the player to dismiss it. Input is routed
    /// to the dismissal while it's up (see <see cref="HandleButton"/>), so Confirm can't also
    /// relaunch the focused tile behind it.
    /// </summary>
    private async Task ShowErrorAsync(IPickerCard card, string title, string? message)
    {
        var wasBusy = _isBusy;
        _isBusy = true;
        Splash.Show(new SplashContent(
            title,
            message ?? string.Empty,
            card,
            Hints: [ButtonPrompts.Hint(ControllerKind, PromptButton.Confirm, "OK")]));

        await WaitForDismissAsync();

        if (!wasBusy)
        {
            Splash.Hide();
            _isBusy = false;
        }
    }

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
}
