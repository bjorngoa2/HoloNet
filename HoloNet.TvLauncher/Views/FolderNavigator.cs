using HoloNet.TvLauncher.Configuration;

namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Owns the picker's folder navigation: which cards are currently on screen, the drill-in path
/// that got there, and how to build the root screen (Games folder + shortcuts) or a folder's
/// contents from the known game library. Extracted out of <see cref="MainWindow"/> so this
/// state/logic is readable and testable independently of gamepad handling, the launch workflow,
/// and the screensaver (see <see cref="ScreensaverController"/>).
/// </summary>
public sealed class FolderNavigator(TvLauncherOptions options)
{
    /// <summary>
    /// Snapshot of a screen pushed onto <see cref="_history"/> when descending into a folder
    /// (see <see cref="EnterFolder"/>), so <see cref="GoBack"/> can restore exactly what was on
    /// screen (including the previously-selected card) rather than rebuilding it.
    /// </summary>
    private sealed record NavFrame(string HeaderTitle, List<IPickerCard> Cards, int SelectedIndex, FolderCardViewModel? Folder);

    private readonly Stack<NavFrame> _history = new();
    private readonly List<GameCardViewModel> _allGameCards = [];

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

    private void ShowRootScreen()
    {
        CurrentFolder = null;
        HeaderTitle = "Home";

        var gamesFolder = new FolderCardViewModel(
            "Games",
            $"{_allGameCards.Count} game{(_allGameCards.Count == 1 ? "" : "s")}",
            BuildPlatformFolders);

        Cards.Clear();
        Cards.Add(gamesFolder);
        Cards.AddRange(options.Shortcuts.Select(s => (IPickerCard)new ShortcutCardViewModel(s)));
    }

    public void EnterFolder(FolderCardViewModel folder, int selectedIndex)
    {
        _history.Push(new NavFrame(HeaderTitle, [.. Cards], selectedIndex, CurrentFolder));

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
                () => g.Cast<IPickerCard>().ToList()))
            .ToList();

    private string DisplayNameForPlatform(string platform) =>
        options.PlatformDisplayNames.TryGetValue(platform, out var displayName) ? displayName : platform;
}
