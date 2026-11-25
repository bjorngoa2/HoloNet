using System.Windows;

namespace HoloNet.TvLauncher.Gallery.Stories;

/// <summary>
/// One state of one of the launcher's views, with made-up data: what the gallery shows and the
/// screenshot tests capture.
/// </summary>
/// <param name="Group">The view it shows, e.g. "Splash".</param>
/// <param name="Name">The state, e.g. "Update ready".</param>
/// <param name="Size">Its size in launcher pixels (the 1920×1080 canvas), and of its screenshot.</param>
/// <param name="CreateView">Builds a fresh view in that state. Call on the UI thread, with the launcher's theme loaded.</param>
public sealed record Story(string Group, string Name, Size Size, Func<FrameworkElement> CreateView)
{
    /// <summary>The full 1920×1080 canvas.</summary>
    public static Size FullScreen { get; } = new(1920, 1080);

    /// <summary>A stable, file-name-safe identifier, e.g. "splash/update-ready".</summary>
    public string Id => $"{Slug(Group)}/{Slug(Name)}";

    public override string ToString() => Id;

    // Lower-case letters and digits, with runs of anything else (spaces, "·", apostrophes) as one hyphen.
    private static string Slug(string text) =>
        string.Join('-', new string([.. text.ToLowerInvariant().Select(c => char.IsAsciiLetterOrDigit(c) ? c : ' ')])
            .Split(' ', StringSplitOptions.RemoveEmptyEntries));
}
