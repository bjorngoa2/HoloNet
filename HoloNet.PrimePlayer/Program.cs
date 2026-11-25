using System.Diagnostics;
using System.IO;
using System.Windows.Automation;

// HoloNet.PrimePlayer: a small standalone tool that automates "start playback, then go fullscreen"
// for a Prime Video or Disney+ title, using your REAL, everyday Firefox install (not a bot-controlled
// browser). This deliberately does NOT use Selenium/Playwright: those drive a specially-patched
// automation build of Firefox that lacks Widevine/EME (DRM) support, so DRM playback can never
// actually start in it - and even Firefox's own `navigator.webdriver` flag being forced `true`
// under WebDriver control can itself stall DRM license negotiation indefinitely. Your everyday
// Firefox already has Widevine provisioned (that's why manually clicking Play there always
// works), so this tool instead:
//
//   1. Launches your real firefox.exe pointed at the title's Prime Video deep link (using your
//      default profile - you're already logged in, no cookie injection needed) - or reuses an
//      open window whose active tab is already showing this exact title (see FindWindowShowing).
//   2. Steals genuine OS keyboard/mouse focus onto that window (see EnsureRealForegroundFocus) -
//      this turned out to be required, not optional: Prime's page-content Play/Resume button
//      only actually starts playback in response to a REAL, OS-level, focused-window input
//      event. Windows UI Automation's InvokePattern.Invoke() - which looks like it should be
//      the "proper", accessibility-API way to click a button - was verified (live, repeatedly)
//      to silently do nothing on this button when the window wasn't genuinely focused; only a
//      real synthesized mouse click at the button's screen coordinates (see ClickViaRealMouse)
//      reliably works. UI-Automation-level actions on the browser's own CHROME (e.g. the address
//      bar) don't have this restriction - only actual web page content does.
//   3. Finds the (Play/Resume) hero button using Windows UI Automation - the same accessibility
//      API screen readers use - rather than parsing the page DOM, waits for it to settle on
//      screen, clicks it as described above, then confirms the player actually appeared (its
//      "Seek" slider), retrying once and exiting non-zero if playback never started.
//   4. For "--restart": once playback has started (regardless of whatever position Prime's
//      server-tracked "resume" state put it at), finds the player's own "Seek" slider control and
//      sets its position to the minimum (0). This is the part that actually forces a restart -
//      earlier attempts at forcing this via a "?autoplay=1&t=0" deep link URL were verified
//      (live) to NOT work: Prime's hero button still showed "Resume" at its previous position
//      after navigating to that URL, proving the button's resume state - and therefore where
//      playback starts - is tracked server-side per account, and is NOT overridable by the t=
//      query parameter from a fresh page load. Driving the player's own Seek control after
//      playback has already started is the only mechanism confirmed (live) to actually work.
//   Disney+ differs only where its player does (see StreamingSite): its /play/{id} deep link
//   starts playback by itself, so step 3 just waits for its "Timeline" slider, and since Firefox
//   starts unprompted autoplay muted, a real click on the player's "Unmute" button follows.
//   HBO Max's /video/watch/{id} link opens its player too, but paused by the same autoplay block,
//   so the real click goes to the player's own Play button instead. An HBO Max movie's link is its
//   /movie/{id} page, whose Play/Resume button is clicked as on Prime.
//   5. Brings the window to the foreground and sends F11 to trigger the browser's native
//      fullscreen (a Firefox-chrome-level shortcut, unrelated to the page/DRM entirely).
//
// Never touches Prime's playback/DRM endpoints or session cookie - purely UI automation (plus
// synthesized real input) of a browser window a human could have driven identically by hand.

const int ExitUsageError = 1;
const int ExitFirefoxNotFound = 2;
const int ExitWindowNotFound = 3;
const int ExitButtonNotFound = 4;
const int ExitPlaybackNotStarted = 5;
const int MaxPlayAttempts = 2;
const byte VkF11 = 0x7A;
const uint KeyEventFKeyUp = 2;
const int ShowWindowRestore = 9;
const uint MouseEventLeftDown = 0x0002;
const uint MouseEventLeftUp = 0x0004;

// "--restart" can appear anywhere after the required positional args - strip it out first so
// the remaining positional args (DeepLinkUrl, Title, [FirefoxExePath]) parse exactly as before.
var restart = args.Any(a => string.Equals(a, "--restart", StringComparison.OrdinalIgnoreCase));
args = args.Where(a => !string.Equals(a, "--restart", StringComparison.OrdinalIgnoreCase)).ToArray();

if (args.Length < 2 || string.IsNullOrWhiteSpace(args[0]) || string.IsNullOrWhiteSpace(args[1]))
{
    Console.WriteLine("Usage: HoloNet.PrimePlayer <DeepLinkUrl> <Title> [--restart] [FirefoxExePath]");
    Console.WriteLine("Example: HoloNet.PrimePlayer https://www.primevideo.com/region/eu/detail/0GZ76HXP3TQS4E42G5JI9KQR6Q \"Drawn together\"");
    Console.WriteLine("  --restart forces playback to start from position 0 instead of resuming, by");
    Console.WriteLine("  clicking Play/Resume as normal and then dragging the player's own Seek control");
    Console.WriteLine("  back to 0 once playback has begun (see the comment at the top of Program.cs).");
    return ExitUsageError;
}

var deepLinkUrl = args[0];
var title = args[1];
var firefoxPath = args.Length > 2 && !string.IsNullOrWhiteSpace(args[2])
    ? args[2]
    : FindFirefoxExecutable();

if (firefoxPath is null)
{
    Console.WriteLine("Could not find firefox.exe. Pass its path explicitly as a third argument.");
    return ExitFirefoxNotFound;
}

if (!Uri.TryCreate(deepLinkUrl, UriKind.Absolute, out var deepLink))
{
    Console.WriteLine($"\"{deepLinkUrl}\" isn't an absolute URL.");
    return ExitUsageError;
}

// The last path segment of a deep link is the title's own ID - a Prime title/episode id (e.g.
// "0GZ76HXP3TQS4E42G5JI9KQR6Q" or "amzn1.dv.gti.<guid>") or a Disney+ or HBO Max episode id - which stays in
// the tab's URL however the site decorates it (region prefix, "ref=" segments, query strings) -
// so it's what identifies "this title's page".
var titleId = deepLink.Segments[^1].Trim('/');
var site = StreamingSite.For(deepLink);
var opensPlayer = site.OpensPlayer(deepLink);

// Reuse an already-open window only if its ACTIVE tab is showing this exact title (the common
// "restart" case - the user is restarting something that's already open/playing/paused) -
// launching a fresh firefox.exe would NOT reuse that tab: Firefox's single-instance remoting just
// opens the URL as a brand-new tab, which backgrounds/pauses whatever tab was actually playing
// (Firefox throttles background tabs). Any other open Prime tab (e.g. the previous episode) must
// NOT be reused: its player is already under way, so it would "succeed" without ever playing the
// title that was actually picked.
var window = FindWindowShowing(titleId);
var reused = window is not null;
if (window is not null)
{
    Console.WriteLine($"Reusing the open tab for \"{title}\": \"{window.Current.Name}\"");
}
else
{
    Console.WriteLine($"Launching {firefoxPath} -> {deepLinkUrl}");
    Process.Start(new ProcessStartInfo
    {
        FileName = firefoxPath,
        Arguments = $"\"{deepLinkUrl}\"",
        UseShellExecute = true
    });

    window = WaitForWindowShowing(titleId, TimeSpan.FromSeconds(30));
    if (window is null)
    {
        Console.WriteLine($"Timed out waiting for a Firefox tab showing \"{title}\".");
        return ExitWindowNotFound;
    }

    Console.WriteLine($"Found window: \"{window.Current.Name}\"");
}

var hwnd = new IntPtr(window.Current.NativeWindowHandle);
EnsureRealForegroundFocus(hwnd);
Thread.Sleep(300);

// If we're reusing a tab, playback may already be under way (e.g. paused mid-episode) rather
// than sitting on the title's detail page - in that case there's no hero Play/Resume button to
// click at all, so check for the player's own on-screen controls first.
var seekSlider = reused ? WaitForSeekSlider(window, site.TimelineSlider, TimeSpan.FromSeconds(2)) : null;
if (seekSlider is not null)
    Console.WriteLine("Playback already under way in the reused tab - no need to click Play.");

if (seekSlider is null && opensPlayer)
{
    // The deep link is the player itself: just wait for it to load and start.
    Console.WriteLine($"Waiting for {site.Name} to start playback...");
    seekSlider = WaitForSeekSlider(window, site.TimelineSlider, TimeSpan.FromSeconds(45));
}

// Prime re-renders the hero while the page settles, so a click can occasionally land before the
// player takes over; one retry covers that without looping forever on a genuinely broken page.
for (var attempt = 1; seekSlider is null && !opensPlayer && attempt <= MaxPlayAttempts; attempt++)
{
    var playButton = WaitForPlayButton(window, TimeSpan.FromSeconds(60));
    if (playButton is null)
    {
        Console.WriteLine("Timed out waiting for the Play/Continue button to appear on screen and become enabled.");
        return ExitButtonNotFound;
    }

    Console.WriteLine($"Clicking \"{playButton.Name}\" (attempt {attempt} of {MaxPlayAttempts})...");
    ClickAtScreenPoint(playButton.ClickPoint.X, playButton.ClickPoint.Y);

    Console.WriteLine("Waiting for playback to start...");
    seekSlider = WaitForSeekSlider(window, site.TimelineSlider, TimeSpan.FromSeconds(20));
}

if (seekSlider is null)
{
    Console.WriteLine("Playback didn't start - the player never appeared.");
    return ExitPlaybackNotStarted;
}

Console.WriteLine("Playback is under way.");

// Firefox blocks unprompted autoplay with sound, so the site starts muted (Disney+) or paused
// (HBO Max); a real click on its own Unmute or Play button is the user gesture that lifts the block.
if (site.AutoplayBlock is { } autoplayBlock)
{
    // A reused tab settled long ago, so a quick look is enough (and its player may be paused).
    var playingWithSound = reused
        ? LiftAutoplayBlock(window, autoplayBlock, timeout: TimeSpan.FromSeconds(3), settleTime: TimeSpan.Zero)
        : LiftAutoplayBlock(window, autoplayBlock, timeout: TimeSpan.FromSeconds(20), settleTime: TimeSpan.FromSeconds(4));
    Console.WriteLine(playingWithSound ? "Playing with sound." : "Couldn't confirm it's playing with sound - leaving the player as-is.");
}

if (restart)
{
    // NOTE: RangeValuePattern.SetValue only updates the accessibility-exposed value - it does
    // NOT actually move Prime's custom (React-driven) seek bar, since the real widget only
    // reacts to genuine OS-level pointer events, not UIA. Verified live: after calling
    // SetValue(Minimum), the slider's own Value read back as 0, but the on-screen elapsed-time
    // text kept climbing exactly as if nothing happened. The only mechanism confirmed to
    // actually reposition playback is a real synthesized mouse click on the seek bar's track,
    // as close to its left edge as possible (clicking exactly at the edge can miss the
    // clickable area, so a few pixels in is used) - verified this jumps playback to ~0:09-0:11
    // elapsed, which is close enough to "the start" for a restart.
    var seekRect = seekSlider.Current.BoundingRectangle;
    var clickX = (int)(seekRect.Left + 5);
    var clickY = (int)(seekRect.Top + seekRect.Height / 2);
    Console.WriteLine($"Restart requested - clicking the Seek bar near its left edge ({clickX}, {clickY}) to jump to the start...");
    ClickAtScreenPoint(clickX, clickY);
    Thread.Sleep(TimeSpan.FromSeconds(1));
}

if (IsFullscreen(hwnd))
{
    Console.WriteLine("Window is already fullscreen - leaving it as-is.");
}
else
{
    Console.WriteLine("Sending F11 for fullscreen...");
    EnsureRealForegroundFocus(hwnd);
    Thread.Sleep(300);
    NativeMethods.keybd_event(VkF11, 0, 0, UIntPtr.Zero);
    Thread.Sleep(50);
    NativeMethods.keybd_event(VkF11, 0, KeyEventFKeyUp, UIntPtr.Zero);
}

// The launcher's curtain stays up until this tool exits, so the site's control bar (brought up
// by the clicks and mouse nudges above) can fade away out of sight.
Console.WriteLine(WaitForPlayerControlsToHide(window, site.TimelineSlider, TimeSpan.FromSeconds(8))
    ? "The player's controls have hidden."
    : "The player's controls are still showing - leaving them.");

Console.WriteLine("Done. Leaving Firefox running.");
return 0;

static string? FindFirefoxExecutable()
{
    string[] candidates =
    [
        @"C:\Program Files\Mozilla Firefox\firefox.exe",
        @"C:\Program Files (x86)\Mozilla Firefox\firefox.exe"
    ];

    return candidates.FirstOrDefault(File.Exists);
}

/// <summary>
/// Grants the target window REAL OS-level keyboard/mouse focus - not just UI Automation's own
/// notion of focus - which turned out to matter a lot: Prime's page-content Play/Resume button
/// only responds to input while its window is the genuine OS foreground window; plain
/// <c>SetForegroundWindow</c> alone is unreliable here (a recurring issue throughout this tool's
/// development - Windows' foreground-lock routinely refuses to hand focus to a window that
/// didn't just spawn from the calling process). The standard, documented workaround is to
/// temporarily attach this process's input queue to whatever window currently owns the
/// foreground (<c>AttachThreadInput</c>), which lets <c>SetForegroundWindow</c> succeed as if we
/// were that process, then detach again immediately after. This was verified live: without it,
/// <c>SetForegroundWindow</c> would "succeed" (return true / no error) yet the OS-reported
/// foreground window would remain unchanged; with it, the target window became the actual
/// foreground window and subsequent real clicks/keystrokes were delivered to it correctly.
/// </summary>
static void EnsureRealForegroundFocus(IntPtr targetHwnd)
{
    var foregroundWindow = NativeMethods.GetForegroundWindow();
    if (foregroundWindow == targetHwnd)
        return;

    var foregroundThreadId = NativeMethods.GetWindowThreadProcessId(foregroundWindow, out _);
    var currentThreadId = NativeMethods.GetCurrentThreadId();

    var attached = foregroundThreadId != 0 && foregroundThreadId != currentThreadId
        && NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, true);
    try
    {
        // Only restore if actually minimized - calling this unconditionally was found to break an
        // already-fullscreen (F11) window's real OS geometry (forcing it out of fullscreen) even
        // though Firefox's own internal fullscreen flag stays set, causing the window to end up
        // stuck in a broken non-fullscreen state that IsFullscreen() still (wrongly) reports as
        // fullscreen (since chrome stays hidden from the accessibility tree either way) - so F11
        // never gets re-sent to fix it. Verified live: this was the cause of "restart works but
        // Firefox doesn't end up fullscreen afterwards."
        if (NativeMethods.IsIconic(targetHwnd))
            NativeMethods.ShowWindow(targetHwnd, ShowWindowRestore);

        NativeMethods.SetForegroundWindow(targetHwnd);
        NativeMethods.BringWindowToTop(targetHwnd);
    }
    finally
    {
        if (attached)
            NativeMethods.AttachThreadInput(currentThreadId, foregroundThreadId, false);
    }
}

/// <summary>
/// Clicks at a screen point using a REAL synthesized mouse click (<c>SetCursorPos</c> +
/// <c>mouse_event</c>) rather than <see cref="InvokePattern"/>. <c>InvokePattern.Invoke()</c> on
/// Prime's Play/Resume button was verified live, repeatedly, to do nothing at all (no
/// page/URL/player change, even after 6+ seconds), while a real click at the exact same button
/// immediately started playback. Web page content apparently requires a real, OS-level input
/// event (carrying real user-activation semantics for Firefox's autoplay/media policies) - UI
/// Automation's accessibility-level "invoke" doesn't carry that, even though it works fine for
/// browser CHROME controls (e.g. the address bar). Also used to click Prime's Seek bar near its
/// left edge, since clicking the actual track is what genuinely moves playback.
/// </summary>
static void ClickAtScreenPoint(int x, int y)
{
    NativeMethods.SetCursorPos(x, y);
    Thread.Sleep(150);
    NativeMethods.mouse_event(MouseEventLeftDown, 0, 0, 0, UIntPtr.Zero);
    Thread.Sleep(80);
    NativeMethods.mouse_event(MouseEventLeftUp, 0, 0, 0, UIntPtr.Zero);
}

/// <summary>
/// Prime's on-screen player controls (Pause, Seek bar, etc.) auto-hide after a few seconds of no
/// mouse movement over the video, and appear to be removed from the accessibility tree entirely
/// while hidden (not just visually faded) - <see cref="WaitForSeekSlider"/> can't find the Seek
/// slider at all until the controls are showing. A tiny mouse move over the middle of the window
/// (real <c>SetCursorPos</c>, same as <see cref="ClickAtScreenPoint"/>) reveals them again, without
/// clicking anything or otherwise affecting playback.
/// </summary>
static void NudgeMouseOver(AutomationElement window)
{
    var rect = window.Current.BoundingRectangle;
    var x = (int)(rect.Left + rect.Width / 2);
    var y = (int)(rect.Top + rect.Height / 2);

    NativeMethods.SetCursorPos(x, y);
    Thread.Sleep(150);
    NativeMethods.SetCursorPos(x + 3, y + 3);
    Thread.Sleep(400);
}

/// <summary>
/// Whether the window exactly covers its monitor - true for both Firefox's own F11 fullscreen and
/// Prime's in-page (DOM) fullscreen, and false for a merely maximized window (which stops at the
/// taskbar and overhangs the monitor edges by its resize border). Checked by geometry rather than
/// by looking for the address bar in the accessibility tree: the address bar stays in the tree
/// while fullscreen, so that check wrongly reported "not fullscreen" and the F11 sent afterwards
/// toggled an already-fullscreen window back OUT of fullscreen.
/// </summary>
static bool IsFullscreen(IntPtr hwnd)
{
    var monitorInfo = new NativeMethods.MonitorInfo { Size = System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.MonitorInfo>() };
    var monitor = NativeMethods.MonitorFromWindow(hwnd, NativeMethods.MonitorDefaultToNearest);
    return NativeMethods.GetWindowRect(hwnd, out var windowRect)
        && NativeMethods.GetMonitorInfo(monitor, ref monitorInfo)
        && windowRect.Equals(monitorInfo.Monitor);
}

/// <summary>
/// Finds a Firefox window whose ACTIVE tab is showing <paramref name="titleId"/>'s page. Window
/// titles can't identify the title: an episode's page is titled after its season
/// ("Prime Video: REACHER (TV) - SEASON 01"), and any other open Prime tab matches a generic
/// "Prime Video" check. Firefox exposes each tab's web content as a
/// <see cref="ControlType.Document"/> whose <see cref="ValuePattern"/> value is the tab's URL;
/// background tabs' documents are reported offscreen, so the visible one is the active tab.
/// With <paramref name="anyTab"/>, a background tab's document counts too.
/// </summary>
static AutomationElement? FindWindowShowing(string titleId, bool anyTab = false)
{
    var documentCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document);
    var top = AutomationElement.RootElement.FindAll(TreeScope.Children, Condition.TrueCondition);

    foreach (AutomationElement candidate in top)
    {
        try
        {
            if (!(candidate.Current.Name ?? string.Empty).Contains("Mozilla Firefox", StringComparison.OrdinalIgnoreCase))
                continue;

            foreach (AutomationElement document in candidate.FindAll(TreeScope.Descendants, documentCondition))
            {
                if ((anyTab || !document.Current.IsOffscreen)
                    && document.TryGetCurrentPattern(ValuePattern.Pattern, out var pattern)
                    && ((ValuePattern)pattern).Current.Value.Contains(titleId, StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }
        }
        catch (ElementNotAvailableException)
        {
            // The window/tab closed or re-rendered mid-search - skip it.
        }
    }

    return null;
}

/// <summary>
/// Waits for a freshly opened tab to show <paramref name="titleId"/>'s page. Firefox doesn't
/// always come to the front when handed a URL by another process, and a window covered by
/// another one (such as the launcher) reports even its active tab offscreen - so a window that
/// has the title open in any tab is brought to the front, letting its active tab show as visible.
/// </summary>
static AutomationElement? WaitForWindowShowing(string titleId, TimeSpan timeout)
{
    var deadline = DateTime.UtcNow + timeout;
    while (DateTime.UtcNow < deadline)
    {
        var found = FindWindowShowing(titleId);
        if (found is not null)
            return found;

        if (FindWindowShowing(titleId, anyTab: true) is { } covered)
            EnsureRealForegroundFocus(new IntPtr(covered.Current.NativeWindowHandle));

        Thread.Sleep(1000);
    }

    return null;
}

/// <summary>
/// Polls for the page's main "Play"/"Resume" hero button and returns it once it's enabled, on
/// screen, and has reported the same bounding rectangle on two consecutive polls. The hero button
/// follows the deep-linked title: an episode's link shows e.g. "Play S1 E2" even though the page
/// itself is the season's.
///
/// The rectangle checks are what make the click land: right after the page loads, Prime's hero
/// button is in the accessibility tree with an EMPTY bounding rectangle while the layout settles.
/// Clicking its "center" then meant clicking screen point (0,0) - Firefox's "Firefox View" button
/// in the window's top-left corner - which switched away from the Prime tab, so nothing ever
/// played (verified live). Only the active tab's page is searched - see <see cref="FindActivePage"/>.
///
/// UI Automation doesn't expose Prime's <c>data-testid</c> attributes, but it *does* expose the
/// element's CSS class list via <see cref="AutomationElement.AutomationElementInformation.ClassName"/>.
/// The hero play button's class list includes both the "fbl-play-btn" AND "fbl-btn" tokens
/// together. Neither token alone is unique: the per-episode row buttons further down the page
/// also carry "fbl-play-btn" (without "fbl-btn"), and other buttons (e.g. "Send feedback") carry
/// "fbl-btn" (without "fbl-play-btn"). Requiring BOTH picks out the hero button regardless of its
/// language- and resume-state-dependent text. A text match is kept as a fallback in case Prime
/// ever changes these class names - and is what finds an HBO Max movie page's Play/Resume button.
/// </summary>
static ScreenButton? WaitForPlayButton(AutomationElement window, TimeSpan timeout)
{
    var buttonCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);
    string[] playTextPrefixes = ["Play", "Resume", "Spill", "Fortsett", "Gjenoppta"];
    System.Windows.Rect? previousRect = null;

    var deadline = DateTime.UtcNow + timeout;
    while (DateTime.UtcNow < deadline)
    {
        AutomationElement? heroButton = null;
        AutomationElement? textMatch = null;
        var page = FindActivePage(window);
        IEnumerable<AutomationElement> candidates = page is null ? [] : page.FindAll(TreeScope.Descendants, buttonCondition).Cast<AutomationElement>();
        foreach (var candidate in candidates)
        {
            try
            {
                var current = candidate.Current;
                if (!current.IsEnabled || current.IsOffscreen)
                    continue;

                var classTokens = (current.ClassName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (classTokens.Contains("fbl-play-btn", StringComparer.Ordinal) && classTokens.Contains("fbl-btn", StringComparer.Ordinal))
                {
                    heroButton = candidate;
                    break;
                }

                // HBO Max wraps its labels in Unicode directional isolates ("\u2066\u2068Gjenoppta ...").
                var name = (current.Name ?? string.Empty).TrimStart('\u2066', '\u2067', '\u2068', '\u2069');
                if (textMatch is null && playTextPrefixes.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    textMatch = candidate;
            }
            catch (ElementNotAvailableException)
            {
                // Element went stale (page re-rendered) - skip it.
            }
        }

        var button = heroButton ?? textMatch;
        System.Windows.Rect? rect = null;
        try
        {
            if (button is not null && button.Current.BoundingRectangle is { IsEmpty: false, Width: > 0, Height: > 0 } bounds)
                rect = bounds;
        }
        catch (ElementNotAvailableException)
        {
        }

        if (button is not null && rect is { } settled && settled == previousRect)
        {
            var center = new System.Drawing.Point((int)(settled.Left + settled.Width / 2), (int)(settled.Top + settled.Height / 2));
            return new ScreenButton(button.Current.Name ?? string.Empty, center);
        }

        previousRect = rect;
        Thread.Sleep(500);
    }

    return null;
}

/// <summary>
/// Polls for the video player's own seek slider (<see cref="ControlType.Slider"/> matching
/// <paramref name="slider"/> - verified live as "Seek" on Prime Video, "Timeline" on Disney+, and
/// an unnamed "ScrubberInput" on HBO Max) - only present once the player has actually loaded (as opposed to still sitting
/// on the title's detail page or the player's loading screen), so this doubles as the "did
/// playback actually start" check. Nudges the mouse on every poll since the controls (and so the
/// slider) auto-hide - see <see cref="NudgeMouseOver"/>.
/// </summary>
static AutomationElement? WaitForSeekSlider(AutomationElement window, ControlMatch slider, TimeSpan timeout)
{
    var deadline = DateTime.UtcNow + timeout;
    while (DateTime.UtcNow < deadline)
    {
        NudgeMouseOver(window);
        if (FindVisibleSlider(window, slider) is { } found)
            return found;

        Thread.Sleep(500);
    }

    return null;
}

/// <summary>
/// Waits up to <paramref name="timeout"/> for the player's own controls to auto-hide - checked by
/// its seek slider leaving the accessibility tree (see <see cref="NudgeMouseOver"/>) - so the
/// viewer isn't handed a video with the site's control bar still drawn over it. The mouse is left
/// still, since any movement brings the controls back.
/// </summary>
static bool WaitForPlayerControlsToHide(AutomationElement window, ControlMatch slider, TimeSpan timeout)
{
    var deadline = DateTime.UtcNow + timeout;
    while (DateTime.UtcNow < deadline)
    {
        if (FindVisibleSlider(window, slider) is null)
            return true;

        Thread.Sleep(250);
    }

    return false;
}

static AutomationElement? FindVisibleSlider(AutomationElement window, ControlMatch slider)
{
    var sliderCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Slider);
    foreach (AutomationElement candidate in window.FindAll(TreeScope.Descendants, sliderCondition))
    {
        try
        {
            if (!candidate.Current.IsOffscreen && slider.Matches(candidate.Current))
                return candidate;
        }
        catch (ElementNotAvailableException)
        {
        }
    }

    return null;
}

/// <summary>
/// Lifts Firefox's block on a site's unprompted autoplay with sound: clicks the site's gesture
/// button - Disney+'s Unmute, or HBO Max's Play (a real click - the user gesture Firefox requires) -
/// until the player has been playing with no gesture button for <paramref name="settleTime"/>.
/// Verified live on Disney+: the seek bar and then the Pause button appear before the player reacts
/// to the browser's autoplay block, and only a second or two later does it switch to muted - so
/// neither alone means "playing with sound". HBO Max instead loads paused, showing its Play button.
/// A reused tab's player may be paused, so its caller passes a short <paramref name="timeout"/>.
/// </summary>
static bool LiftAutoplayBlock(AutomationElement window, AutoplayBlock controls, TimeSpan timeout, TimeSpan settleTime)
{
    var deadline = DateTime.UtcNow + timeout;
    DateTime? playingWithSoundSince = null;
    while (DateTime.UtcNow < deadline)
    {
        NudgeMouseOver(window);
        if (controls.GestureButtonNames.Select(name => FindVisibleButton(window, name)).FirstOrDefault(button => button is not null) is { } gestureButton)
        {
            Console.WriteLine($"Clicking \"{gestureButton.Name}\"...");
            ClickAtScreenPoint(gestureButton.ClickPoint.X, gestureButton.ClickPoint.Y);
            playingWithSoundSince = null;
            Thread.Sleep(1000);
            continue;
        }

        if (FindVisibleButton(window, controls.PauseButtonName) is null)
            playingWithSoundSince = null;
        else if ((playingWithSoundSince ??= DateTime.UtcNow) + settleTime <= DateTime.UtcNow)
            return true;

        Thread.Sleep(250);
    }

    return false;
}

/// <summary>
/// The active tab's web page - the first <see cref="ControlType.Document"/> in the window that's on
/// screen - or <c>null</c> while none is. Controls are only ever looked for in the page, never in
/// Firefox's own toolbars: its tab strip gives every tab whose media autoplay it blocked a
/// "Play tab" button ("Spill fane" in Norwegian) - verified live with a few HBO Max tabs open - which
/// a "Play"/"Spill" text match would otherwise click, switching away from the tab being played.
/// </summary>
static AutomationElement? FindActivePage(AutomationElement window)
{
    var documentCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Document);
    foreach (AutomationElement document in window.FindAll(TreeScope.Descendants, documentCondition))
    {
        try
        {
            if (!document.Current.IsOffscreen)
                return document;
        }
        catch (ElementNotAvailableException)
        {
        }
    }

    return null;
}

/// <summary>
/// Finds an enabled, on-screen button named exactly <paramref name="name"/> with a real (non-empty)
/// bounding rectangle in the active tab's page - see <see cref="WaitForPlayButton"/> for why an
/// empty rectangle must never be clicked, and <see cref="FindActivePage"/> for why only the page.
/// </summary>
static ScreenButton? FindVisibleButton(AutomationElement window, string name)
{
    var buttonCondition = new AndCondition(
        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
        new PropertyCondition(AutomationElement.NameProperty, name));

    if (FindActivePage(window) is not { } page)
        return null;

    foreach (AutomationElement candidate in page.FindAll(TreeScope.Descendants, buttonCondition))
    {
        try
        {
            var current = candidate.Current;
            if (current.IsEnabled && !current.IsOffscreen
                && current.BoundingRectangle is { IsEmpty: false, Width: > 0, Height: > 0 } bounds)
            {
                return new ScreenButton(name, new System.Drawing.Point((int)(bounds.Left + bounds.Width / 2), (int)(bounds.Top + bounds.Height / 2)));
            }
        }
        catch (ElementNotAvailableException)
        {
        }
    }

    return null;
}

/// <summary>A button found on screen, with the settled screen point to click.</summary>
internal sealed record ScreenButton(string Name, System.Drawing.Point ClickPoint);

internal static class NativeMethods
{
    public const uint MonitorDefaultToNearest = 2;

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public record struct Rect(int Left, int Top, int Right, int Bottom);

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public struct MonitorInfo
    {
        public int Size;
        public Rect Monitor;
        public Rect WorkArea;
        public uint Flags;
    }

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out Rect lpRect);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MonitorInfo lpmi);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [System.Runtime.InteropServices.DllImport("kernel32.dll")]
    public static extern uint GetCurrentThreadId();

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool BringWindowToTop(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool IsIconic(IntPtr hWnd);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [System.Runtime.InteropServices.DllImport("user32.dll")]
    public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
}
