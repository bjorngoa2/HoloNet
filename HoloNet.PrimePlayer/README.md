# HoloNet.PrimePlayer

A small standalone tool that automates "click Play, then go fullscreen" for a Prime Video, Disney+ or HBO Max title,
using your **real, everyday Firefox install** - so selecting a movie/episode in
`HoloNet.TvLauncher` starts playing immediately instead of just opening a browser tab you then
have to click through yourself.
The site is picked from the deep link's host (`StreamingSite`); anything that isn't Disney+ or HBO Max is treated as Prime Video.

## Why this exists (and why not Selenium/Playwright)

`HoloNet.TvLauncher`'s Prime Video integration is deliberately catalog-only - it never touches
Prime's playback/DRM endpoints (see `HoloNet.Video/Providers/PrimeVideo`), so selecting a
movie/episode just opens its deep link in a browser and leaves the rest to you.

Automating the rest (clicking Play, going fullscreen) sounds like a job for a browser automation
tool like Selenium or Playwright - but both of those drive a specially-patched *automation build*
of the browser that deliberately lacks Widevine/EME (DRM) support, so a Playwright/Selenium-driven
Firefox can click every button correctly and the video will still never actually play. Firefox
also forces `navigator.webdriver = true` for any WebDriver-controlled session, which can itself
stall DRM license negotiation indefinitely even if you work around the missing-CDM problem with a
persistent profile.

Your real, everyday Firefox already has Widevine provisioned (that's exactly why manually
clicking Play there has always worked) and isn't flagged as automated at all. So instead of
driving a fake browser, this tool drives your **real** Firefox window using **Windows UI
Automation** - the same accessibility API screen readers use to read and interact with a page.
From the browser's point of view, this is indistinguishable from an assistive-technology user (or
you, manually) clicking the button - because it is the same API surface.

## What it does

1. Reuses an open Firefox window if its **active tab** is already showing this exact title (matched by the title ID at the end of the deep link, read from the tab's URL via UI Automation).
   Otherwise it launches `firefox.exe` (your default profile - you're already logged in, no cookie injection needed) pointed at the title's Prime Video deep link, and waits for a tab showing it.
   Window titles can't be used for this: an episode's page is titled after its season, and any other open Prime tab (e.g. the previous episode) would match a generic check.
2. Brings the window to the real OS foreground.
   Firefox doesn't always come to the front when handed a URL, and a window covered by another one (such as the launcher) reports even its active tab as offscreen.
   So when the title is only open in a tab that isn't showing, the window holding it is brought to the front first.
3. Finds the page's hero Play/Resume button via UI Automation.
   For an episode link, that button follows the episode (e.g. `Play S1 E2`), even though the page is the season's.
   It waits until the button is enabled, on screen, and reports the same non-empty bounding rectangle twice in a row - right after loading, the button briefly reports an empty rectangle, and clicking its "centre" then hit Firefox's own top-left toolbar button instead.
   Buttons are only looked for in the active tab's page, never in Firefox's toolbars: the tab strip gives each tab whose autoplay Firefox blocked a "Play tab" button, which a text match would otherwise click.
4. Clicks it with a real synthesized mouse click (`InvokePattern.Invoke()` was verified not to start playback).
5. Confirms playback actually started by waiting for the player's `Seek` slider, retrying the click once.
   If the player never appears, it exits with a non-zero code so HoloNet.TvLauncher can fall back.
6. With `--restart`, clicks the Seek bar near its left edge to jump back to the start.
7. Sends **F11** for Firefox's native fullscreen, unless the window already covers its whole monitor.
8. Waits (up to 8 seconds) for the player's own controls, brought up by the clicks above, to auto-hide, so the viewer gets a clean picture when the launcher's curtain lifts.
9. Exits, leaving Firefox running independently - this tool never calls anything to close it.

### Disney+

A Disney+ `/play/{id}` link starts playback by itself, so steps 3-4 are skipped and the tool waits for the player's `Timeline` slider instead.
Firefox blocks autoplay with sound, so Disney+ switches itself to muted a second or two after playback starts.
The tool clicks the player's **Unmute** button (a real click is the user gesture Firefox needs) until the player has been playing for 4 seconds with no Unmute button showing.

### HBO Max

An episode's `/video/watch/{id}` link opens the player too, but the same autoplay block leaves it paused.
The tool clicks the player's own **Play** button (`Spill av` in Norwegian) until it has been playing for 4 seconds; while the stream is still buffering, the site ignores those clicks, so a few are normal.
The seek bar has no accessible name, so it is matched by its CSS class (`ScrubberInput…`), which UI Automation exposes as the element's class name.
A movie's playable video has an ID that only signed-in pages reveal, so a movie links to its `/movie/{id}` page instead, and the tool clicks that page's Play/Resume button as it does on Prime Video.

Never touches Prime's playback/DRM endpoints or session cookie - purely UI automation of a completely normal, non-automated browser window.

## Running it

```powershell
dotnet run --project HoloNet.PrimePlayer -- "https://www.primevideo.com/region/eu/detail/<id>" "Title" [--restart]
```

The title argument is only used in log messages - it's the same `Title` HoloNet.Video's catalog already returns for the item.

Optionally pass a third argument to override the Firefox executable path if it's not installed at one of the usual locations (`C:\Program Files\Mozilla Firefox\firefox.exe` or the `(x86)` equivalent).

| Exit code | Meaning |
|---|---|
| 0 | Playback started |
| 1 | Bad arguments |
| 2 | Firefox not found |
| 3 | No Firefox tab showing the title appeared within 30 seconds |
| 4 | The Play/Resume button never appeared on screen |
| 5 | The button was clicked, but the player never appeared |

## Triggering it from HoloNet.TvLauncher

Selecting a movie/episode card calls `IPrimePlayerLauncher.PlayAsync` (see
`HoloNet.TvLauncher/Services/PrimePlayerLauncher.cs`), which launches this tool as a child
process and awaits it exiting - same "start a process, wait for it to close" shape as
`IPrimeAuthLauncher`/`IGameLauncher`.
Release builds (`.github/workflows/release.yml`) publish this tool into the launcher's own folder, and `appsettings.json` defaults `TvLauncher:PrimePlayerToolPath` to `HoloNet.PrimePlayer.exe`.
A relative path is resolved against the launcher's folder, so an installed launcher finds the bundled tool with no setup.
For development, point it at your build output in `appsettings.local.json` instead.
If it's not configured, isn't
installed, or fails, the picker falls back to just opening the deep link in the default browser
(the previous behavior) so a title is never unplayable.

While the tool runs, the launcher keeps its "Starting…" splash on top of Firefox in a click-through, always-on-top window (`PlaybackCurtainWindow`), and fades it out once playback has started.
The player never sees the page being clicked through - the title opens straight into fullscreen playback, like a native app.
