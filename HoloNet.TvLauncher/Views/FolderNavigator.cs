using System.Diagnostics.CodeAnalysis;
using HoloNet.TvLauncher.Configuration;
using HoloNet.TvLauncher.Services;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Owns the picker's folder navigation: which cards are currently on screen, the drill-in path
/// that got there, and how to build the root screen (Games folder, streaming folders, shortcuts
/// and Settings) or a folder's
/// contents from the known game library. Extracted out of <see cref="MainWindow"/> so this
/// state/logic is readable and testable independently of gamepad handling, the launch workflow,
/// and the screensaver (see <see cref="ScreensaverController"/>).
/// </summary>
/// <remarks>
/// It also owns rearranging the home screen (see <see cref="BeginMove"/>), because a library
/// refresh can rebuild the home screen in the middle of a move, and the tile being moved has to
/// stay where the player put it.
/// </remarks>
public sealed class FolderNavigator(TvLauncherOptions options, ILauncherSettingsStore settingsStore, SettingsMenu settingsMenu)
{
    /// <summary>
    /// Snapshot of a screen pushed onto <see cref="_history"/> when descending into a folder
    /// (see <see cref="EnterFolder"/>), so <see cref="GoBack"/> can restore exactly what was on
    /// screen (including the previously-selected card) rather than rebuilding it.
    /// </summary>
    private sealed record NavFrame(string HeaderTitle, List<IPickerCard> Cards, int SelectedIndex, FolderCardViewModel? Folder);

    private const string GamesAccentColor = "#7B5CFF";
    private const string GamesNavigationKey = "games";

    private readonly Stack<NavFrame> _history = new();

    /// <summary>
    /// The stable key of each home tile the player can move (everything but Settings, which stays
    /// last), used to save and restore their order. Filled while the home screen is built.
    /// </summary>
    private readonly Dictionary<IPickerCard, string> _homeTileKeys = new(ReferenceEqualityComparer.Instance);

    /// <summary>The key of the tile being moved, or <c>null</c> when not rearranging.</summary>
    private string? _movingKey;

    /// <summary>The order so far during a move, so a rebuild mid-move keeps it.</summary>
    private IReadOnlyList<string>? _orderDuringMove;
    private readonly List<GameCardViewModel> _allGameCards = [];

    /// <summary>Each streaming service's titles, keyed by service (see <see cref="SetStreamingItems"/>).</summary>
    private readonly Dictionary<StreamingService, List<StreamingTitle>> _streamingTitles = [];

    /// <summary>
    /// <see cref="FolderCardViewModel.NavigationKey"/> of each folder entered to reach the
    /// current screen, root-to-leaf order, in lockstep with <see cref="_history"/> - lets
    /// <see cref="RebuildCurrentPath"/> replay the same drill-in path against a
    /// freshly-fetched streaming card tree after <see cref="SetStreamingItems"/> (rather than
    /// re-invoking the current, now-stale folder's own children closure, which would keep
    /// showing whatever progress/resume state existed when that folder was first entered).
    /// </summary>
    private readonly List<string?> _currentPathKeys = [];

    /// <summary>
    /// The cards for whichever screen is currently on display. One mutable list reused across
    /// screens (root, a folder's contents, going back) rather than a fresh list per screen, so
    /// callers (e.g. <see cref="MainWindow"/>'s <c>ItemsControl</c> binding) can hold a single
    /// stable reference.
    /// </summary>
    public List<IPickerCard> Cards { get; } = [];

    public FolderCardViewModel? CurrentFolder { get; private set; }

    public string HeaderTitle { get; private set; } = "Home";

    public bool CanGoBack => _history.Count > 0;

    /// <summary>
    /// Replaces the known game library (e.g. after a fresh fetch from the Games API) and
    /// rebuilds whichever screen is currently on display: the root screen if not currently
    /// inside a folder, or just the current folder's contents if browsing one — refreshing
    /// never bounces the player back out to the root screen.
    /// </summary>
    public void SetGames(IEnumerable<GameCardViewModel> games)
    {
        _allGameCards.Clear();
        _allGameCards.AddRange(games);

        if (CurrentFolder is null)
        {
            _history.Clear();
            _currentPathKeys.Clear();
            ShowRootScreen();
        }
        else
        {
            // The current folder's children factory reads live from _allGameCards, so
            // re-invoking it after the update above already reflects the fresh data — no need
            // to rebuild the whole navigation path from scratch.
            Cards.Clear();
            Cards.AddRange(CurrentFolder.GetChildren());
        }
    }

    /// <summary>
    /// Replaces a streaming service's titles (its HoloNet.Video library entries) and
    /// rebuilds whichever screen is currently on display, mirroring <see cref="SetGames"/> - except, unlike Games,
    /// a season/episode-list folder's children factory closes over a fixed snapshot of
    /// <see cref="MediaItemDto"/>s taken when that folder was created (see
    /// <see cref="MediaCardViewModel.Create"/>), so simply re-invoking the current folder's own
    /// <see cref="FolderCardViewModel.GetChildren"/> would keep showing stale progress/resume
    /// state forever. <see cref="RebuildCurrentPath"/> re-walks the same drill-in path
    /// against the fresh tree instead, whenever the player is currently browsing under that
    /// service.
    /// </summary>
    public void SetStreamingItems(StreamingService service, IEnumerable<StreamingTitle> titles)
    {
        _streamingTitles[service] = [.. titles];

        if (CurrentFolder is null)
        {
            _history.Clear();
            _currentPathKeys.Clear();
            ShowRootScreen();
        }
        else if (_currentPathKeys.Count > 0 && _currentPathKeys[0] == service.RootNavigationKey)
        {
            RebuildCurrentPath();
        }
        else
        {
            // Browsing something other than this service (e.g. Games) - unaffected, same
            // live-closure behavior as SetGames.
            Cards.Clear();
            Cards.AddRange(CurrentFolder.GetChildren());
        }
    }

    /// <summary>
    /// Replays <see cref="_currentPathKeys"/> (the sequence of <see cref="FolderCardViewModel.NavigationKey"/>s
    /// chosen to reach the current screen) against the just-refreshed <see cref="_streamingTitles"/>
    /// tree, rebuilding <see cref="CurrentFolder"/>/<see cref="Cards"/>/<see cref="HeaderTitle"/>/
    /// <see cref="_history"/> from scratch one level at a time via the normal
    /// <see cref="EnterFolder"/> - so every level below root is a brand-new
    /// <see cref="FolderCardViewModel"/>/<see cref="MediaCardViewModel"/> built from the fresh
    /// fetch, carrying whatever progress just changed. Stops (staying on the deepest ancestor
    /// still resolvable) if a step's key is no longer present - e.g. a title left the configured
    /// watchlist between refreshes - rather than erroring or bouncing all the way back to Home.
    /// </summary>
    private void RebuildCurrentPath()
    {
        // Oldest-first (root descent order) - _history enumerates LIFO (most recent push
        // first), so this walks it back to front to line up with _currentPathKeys' order.
        var previousSelections = _history.Reverse().Select(f => f.SelectedIndex).ToList();
        var keysToReplay = new List<string?>(_currentPathKeys);

        _history.Clear();
        _currentPathKeys.Clear();
        ShowRootScreen();

        for (var depth = 0; depth < keysToReplay.Count; depth++)
        {
            var key = keysToReplay[depth];
            var match = key is null ? null : Cards.OfType<FolderCardViewModel>().FirstOrDefault(f => f.NavigationKey == key);
            if (match is null)
                break;

            var selectedIndex = depth < previousSelections.Count ? previousSelections[depth] : 0;
            EnterFolder(match, selectedIndex);
        }
    }

    private void ShowRootScreen()
    {
        CurrentFolder = null;
        HeaderTitle = "Home";

        var gamesFolder = new FolderCardViewModel(
            "Games",
            $"{_allGameCards.Count} game{(_allGameCards.Count == 1 ? "" : "s")}",
            BuildPlatformFolders,
            navigationKey: GamesNavigationKey,
            art: new CardArt(
                IconGlyph: TileIcons.Game,
                AccentColor: GamesAccentColor,
                Description: "Your retro game library. Pick a console, then a game to play."));

        List<(string Key, IPickerCard Card)> movableTiles =
        [
            (GamesNavigationKey, gamesFolder),
            .. StreamingService.All
                .Where(service => service.IsEnabled(options))
                .Select(service => (service.RootNavigationKey, (IPickerCard)BuildStreamingFolder(service))),
            .. options.Shortcuts.Select(shortcut => ($"shortcut:{shortcut.Title}", (IPickerCard)new ShortcutCardViewModel(shortcut)))
        ];

        _homeTileKeys.Clear();
        Cards.Clear();
        foreach (var (key, card) in HomeOrder.Arrange(movableTiles, tile => tile.Key, _orderDuringMove ?? settingsStore.Current.HomeOrder))
        {
            _homeTileKeys[card] = key;
            card.IsMoving = key == _movingKey;
            Cards.Add(card);
        }

        Cards.Add(settingsMenu.CreateFolder());
    }

    /// <summary>Whether a tile is being moved (see <see cref="BeginMove"/>).</summary>
    public bool IsMoving => _movingKey is not null;

    /// <summary>Where the tile being moved currently sits, or <c>null</c> when not moving one.</summary>
    public int? MovingIndex => Cards.FindIndex(card => card.IsMoving) is var index and >= 0 ? index : null;

    /// <summary>
    /// Whether the tile at <paramref name="index"/> can be picked up: any home tile except
    /// Settings, which stays last so it's always in the same place.
    /// </summary>
    public bool CanMove(int index) =>
        CurrentFolder is null && !IsMoving && index >= 0 && index < Cards.Count && _homeTileKeys.ContainsKey(Cards[index]);

    /// <summary>Picks up the home tile at <paramref name="index"/>, ready to <see cref="MoveTo"/> a new spot.</summary>
    public void BeginMove(int index)
    {
        if (!CanMove(index))
            throw new InvalidOperationException($"The tile at {index} can't be moved.");

        _movingKey = _homeTileKeys[Cards[index]];
        _orderDuringMove = HomeTileKeysInOrder();
        Cards[index].IsMoving = true;
    }

    /// <summary>
    /// Moves the tile being moved to <paramref name="targetIndex"/>, shifting the tiles in between
    /// along by one. It can't go past the last movable tile, so Settings stays last.
    /// </summary>
    /// <returns>The tile's new index.</returns>
    public int MoveTo(int targetIndex)
    {
        if (MovingIndex is not { } from)
            throw new InvalidOperationException("No tile is being moved.");

        var to = Math.Clamp(targetIndex, 0, _homeTileKeys.Count - 1);
        var card = Cards[from];
        Cards.RemoveAt(from);
        Cards.Insert(to, card);
        _orderDuringMove = HomeTileKeysInOrder();
        return to;
    }

    /// <summary>Puts the tile down where it is and saves the new home order.</summary>
    /// <returns>The tile's index.</returns>
    public async Task<int> CommitMoveAsync()
    {
        var index = MovingIndex ?? 0;
        var order = HomeTileKeysInOrder();
        EndMove();
        await settingsStore.SaveAsync(settingsStore.Current with { HomeOrder = order });
        return index;
    }

    /// <summary>Puts every tile back where it was before the move, without saving.</summary>
    /// <returns>The moved tile's original index.</returns>
    public int CancelMove()
    {
        var key = _movingKey;
        _movingKey = null;
        _orderDuringMove = null;
        ShowRootScreen();
        return Math.Max(0, Cards.FindIndex(card => _homeTileKeys.GetValueOrDefault(card) == key));
    }

    private void EndMove()
    {
        foreach (var card in Cards)
            card.IsMoving = false;

        _movingKey = null;
        _orderDuringMove = null;
    }

    private List<string> HomeTileKeysInOrder() =>
        [.. Cards.Where(_homeTileKeys.ContainsKey).Select(card => _homeTileKeys[card])];

    /// <summary>A service's titles, newest first, as last set by <see cref="SetStreamingItems"/>.</summary>
    public IReadOnlyList<StreamingTitle> StreamingTitles(StreamingService service) =>
        _streamingTitles.GetValueOrDefault(service) ?? [];

    /// <summary>
    /// Finds the library title behind <paramref name="card"/>, when it's one of the titles listed
    /// directly in the streaming folder on screen. Seasons and episodes inside a title aren't
    /// library entries of their own, so they never match.
    /// </summary>
    public bool TryGetLibraryTitle(
        IPickerCard card,
        [NotNullWhen(true)] out StreamingService? service,
        [NotNullWhen(true)] out StreamingTitle? title)
    {
        service = StreamingService.All.FirstOrDefault(s => _currentPathKeys is [var key] && key == s.RootNavigationKey);
        title = service is null ? null : StreamingTitles(service).FirstOrDefault(t => ReferenceEquals(t.Card, card));
        return title is not null;
    }

    /// <summary>
    /// A service's root folder: its leading action tiles (e.g. Prime Video's login refresh), its
    /// titles, then an "Add a title" tile. It's shown whenever the service is enabled, even
    /// before (or without) any titles resolving, so those actions are always reachable.
    /// </summary>
    private FolderCardViewModel BuildStreamingFolder(StreamingService service)
    {
        var titles = StreamingTitles(service);
        return new FolderCardViewModel(
            service.Name,
            titles.Count > 0 ? $"{titles.Count} title{(titles.Count == 1 ? "" : "s")}" : service.EmptySubtitle,
            () =>
            [
                .. service.CreateLeadingCards(),
                .. StreamingTitles(service).Select(title => title.Card),
                new AddTitleActionCardViewModel(service)
            ],
            navigationKey: service.RootNavigationKey,
            art: new CardArt(
                IconGlyph: TileIcons.Tv,
                AccentColor: service.AccentColor,
                Description: service.Description));
    }

    public void EnterFolder(FolderCardViewModel folder, int selectedIndex)
    {
        _history.Push(new NavFrame(HeaderTitle, [.. Cards], selectedIndex, CurrentFolder));
        _currentPathKeys.Add(folder.NavigationKey);

        CurrentFolder = folder;
        HeaderTitle = folder.Title;
        Cards.Clear();
        Cards.AddRange(folder.GetChildren());
    }

    /// <summary>
    /// Pops the previous screen off the navigation history and restores it. Only valid to call
    /// when <see cref="CanGoBack"/> is <c>true</c>.
    /// </summary>
    /// <returns>The selected-index to restore on the restored screen, or -1 if it's empty.</returns>
    public int GoBack()
    {
        var frame = _history.Pop();
        _currentPathKeys.RemoveAt(_currentPathKeys.Count - 1);
        CurrentFolder = frame.Folder;
        HeaderTitle = frame.HeaderTitle;
        Cards.Clear();
        Cards.AddRange(frame.Cards);

        return Cards.Count > 0 ? Math.Clamp(frame.SelectedIndex, 0, Cards.Count - 1) : -1;
    }

    private List<IPickerCard> BuildPlatformFolders() =>
        _allGameCards
            .GroupBy(c => c.Platform, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => DisplayNameForPlatform(g.Key), StringComparer.OrdinalIgnoreCase)
            .Select(g => (IPickerCard)new FolderCardViewModel(
                DisplayNameForPlatform(g.Key),
                $"{g.Count()} game{(g.Count() == 1 ? "" : "s")}",
                () => g.Cast<IPickerCard>().ToList(),
                art: new CardArt(IconGlyph: TileIcons.Game, AccentColor: GamesAccentColor)))
            .ToList();

    private string DisplayNameForPlatform(string platform) =>
        options.PlatformDisplayNames.TryGetValue(platform, out var displayName) ? displayName : platform;
}
