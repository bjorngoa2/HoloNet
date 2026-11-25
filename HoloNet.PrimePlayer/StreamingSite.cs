using System.Windows.Automation;

/// <summary>
/// How a streaming site's web player behaves, as far as this tool's automation is concerned.
/// Each site is matched by its deep link's host (see <see cref="For"/>).
/// </summary>
/// <param name="Name">Display name, for logging.</param>
/// <param name="PlayerPath">The path segment of a deep link that opens the player itself (a
/// Disney+ <c>/play/{id}</c> or HBO Max <c>/video/watch/{id}</c> link), or <c>null</c> when every
/// deep link is a title page with a Play button to click (a Prime Video <c>/detail/{id}</c> link,
/// or an HBO Max <c>/movie/{id}</c> one) - see <see cref="OpensPlayer"/>.</param>
/// <param name="TimelineSlider">The player's seek bar - only present once the player has loaded,
/// so it doubles as the "playback started" check.</param>
/// <param name="AutoplayBlock">How to get past Firefox's block on unprompted autoplay with sound,
/// or <c>null</c> when playback always starts from a real click and so is never blocked.</param>
internal sealed record StreamingSite(string Name, string? PlayerPath, ControlMatch TimelineSlider, AutoplayBlock? AutoplayBlock)
{
    public static StreamingSite PrimeVideo { get; } = new(
        "Prime Video", PlayerPath: null, TimelineSlider: new ControlMatch(Name: "Seek"), AutoplayBlock: null);

    /// <summary>Disney+ starts muted when its autoplay is blocked, so its Unmute button lifts the block.</summary>
    public static StreamingSite DisneyPlus { get; } = new(
        "Disney+", PlayerPath: "/play/", TimelineSlider: new ControlMatch(Name: "Timeline"),
        new AutoplayBlock(GestureButtonNames: ["Unmute"], PauseButtonName: "Pause"));

    /// <summary>
    /// HBO Max stays paused when its autoplay is blocked, so its own Play button lifts the block.
    /// Its controls are labelled in the profile's language ("Spill av" in Norwegian), and its seek
    /// bar has no accessible name at all, so the bar is matched by its CSS class instead.
    /// </summary>
    public static StreamingSite HboMax { get; } = new(
        "HBO Max", PlayerPath: "/video/watch/", TimelineSlider: new ControlMatch(ClassNamePrefix: "ScrubberInput"),
        new AutoplayBlock(GestureButtonNames: ["Spill av", "Play"], PauseButtonName: "Pause"));

    /// <summary>Whether opening <paramref name="deepLink"/> opens this site's player itself, rather
    /// than a title page whose Play button must be clicked.</summary>
    public bool OpensPlayer(Uri deepLink) =>
        PlayerPath is not null && deepLink.AbsolutePath.Contains(PlayerPath, StringComparison.OrdinalIgnoreCase);

    /// <summary>The site serving <paramref name="deepLink"/>; anything that isn't Disney+ or HBO
    /// Max is treated as Prime Video, this tool's original (and default) site.</summary>
    public static StreamingSite For(Uri deepLink) =>
        IsOnDomain(deepLink, "disneyplus.com") ? DisneyPlus
        : IsOnDomain(deepLink, "hbomax.com") ? HboMax
        : PrimeVideo;

    private static bool IsOnDomain(Uri uri, string domain) =>
        uri.Host.Equals(domain, StringComparison.OrdinalIgnoreCase)
        || uri.Host.EndsWith($".{domain}", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// Identifies a player control by its accessible name or, for controls without one, by its CSS
/// class: UI Automation exposes a web element's class list as its class name, and styled-component
/// classes start with a stable component name followed by a build-specific hash
/// (<c>ScrubberInput-Fuse-Web-Play__sc-a926096f-0</c>), so only the prefix is matched.
/// </summary>
internal sealed record ControlMatch(string? Name = null, string? ClassNamePrefix = null)
{
    public bool Matches(AutomationElement.AutomationElementInformation control) =>
        (Name is null || string.Equals(control.Name, Name, StringComparison.OrdinalIgnoreCase))
        && (ClassNamePrefix is null
            || (control.ClassName ?? string.Empty)
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                .Any(token => token.StartsWith(ClassNamePrefix, StringComparison.Ordinal)));

    public override string ToString() => Name ?? $"{ClassNamePrefix}*";
}

/// <summary>
/// The player controls involved in getting past Firefox's block on unprompted autoplay with sound,
/// which only a real user gesture lifts.
/// </summary>
/// <param name="GestureButtonNames">The accessible names of the button whose click lifts the block -
/// Disney+'s Unmute, or HBO Max's Play - in each language the site's UI may be in.</param>
/// <param name="PauseButtonName">The accessible name of the player's Pause button - only shown
/// while video is actually playing.</param>
internal sealed record AutoplayBlock(IReadOnlyList<string> GestureButtonNames, string PauseButtonName);
