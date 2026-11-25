namespace HoloNet.TvLauncher.Gallery.Stories;

/// <summary>Every story, in the order the gallery lists them.</summary>
public static class StoryCatalog
{
    public static IReadOnlyList<Story> All { get; } = [.. TileStories.All, .. SplashStories.All];
}
