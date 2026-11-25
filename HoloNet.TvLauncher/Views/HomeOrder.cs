namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Applies the player's saved home tile order (see <see cref="Models.LauncherSettings.HomeOrder"/>).
/// Tiles are matched by a stable key rather than by position, so the order survives tiles being
/// added or removed in <c>appsettings.json</c>.
/// </summary>
public static class HomeOrder
{
    /// <summary>
    /// Sorts <paramref name="tiles"/> by where their key sits in <paramref name="savedOrder"/>.
    /// Tiles the saved order doesn't know yet (e.g. a newly enabled service) keep their default
    /// relative order after the arranged ones, and saved keys with no tile are ignored.
    /// </summary>
    public static List<T> Arrange<T>(IEnumerable<T> tiles, Func<T, string> keyOf, IReadOnlyList<string>? savedOrder)
    {
        var positions = new Dictionary<string, int>(StringComparer.Ordinal);
        for (var i = 0; i < (savedOrder?.Count ?? 0); i++)
            positions.TryAdd(savedOrder![i], i);

        return [.. tiles.OrderBy(tile => positions.TryGetValue(keyOf(tile), out var position) ? position : int.MaxValue)];
    }
}
