namespace HoloNet.TvLauncher.Views;

/// <summary>
/// Icon glyphs for tiles without artwork, from the Windows system icon font ("Segoe Fluent
/// Icons" on Windows 11, "Segoe MDL2 Assets" on Windows 10 - both share these code points, see
/// the <c>IconFont</c> resource in <c>App.xaml</c>). Using the OS icon font keeps tiles crisp at
/// any TV resolution without shipping image assets.
/// </summary>
public static class TileIcons
{
    public const string Game = "\uE7FC";
    public const string Video = "\uE714";
    public const string Tv = "\uE7F4";
    public const string Photo = "\uE91B";
    public const string Play = "\uE768";
    public const string Folder = "\uE8B7";
    public const string Key = "\uE8D7";
    public const string Globe = "\uE774";
    public const string Music = "\uEC4F";
    public const string Add = "\uE710";
    public const string Settings = "\uE713";
    public const string Check = "\uE73E";
    public const string Clock = "\uE823";
    public const string Screensaver = "\uE708";

    private static readonly Dictionary<string, string> ByName = new(StringComparer.OrdinalIgnoreCase)
    {
        [nameof(Game)] = Game,
        [nameof(Video)] = Video,
        [nameof(Tv)] = Tv,
        [nameof(Photo)] = Photo,
        [nameof(Play)] = Play,
        [nameof(Folder)] = Folder,
        [nameof(Key)] = Key,
        [nameof(Globe)] = Globe,
        [nameof(Music)] = Music
    };

    /// <summary>
    /// Resolves a configured icon name (see <see cref="Configuration.ShortcutMapping.Icon"/>),
    /// or <c>null</c> if it's empty or unknown - the tile then falls back to the title's initials.
    /// </summary>
    public static string? FromName(string? name) =>
        name is not null && ByName.TryGetValue(name, out var glyph) ? glyph : null;
}
