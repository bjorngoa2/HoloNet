# HoloNet.TvLauncher

A fullscreen WPF "console" picker that runs on the TV-connected PC. It lists games from
`HoloNet.Games`, lets you browse with a gamepad (D-pad/left-stick + A/B/Start) or arrow
keys/Enter/Escape/F5, launches the mapped emulator over the game's network share, and lets
you quit back to the picker mid-game with a controller hold-combo.

## How it works

1. On startup, fetches `GET api/v1/games` from `HoloNet.Games` and shows a cover-grid picker.
2. Gamepad input works with **both** Xbox-style and PlayStation (DualShock/DualSense)
   controllers, no extra drivers needed:
   - **XInput** (`xinput1_4.dll`, ships with Windows) is tried first — covers Xbox-compatible
     pads.
   - If no XInput controller is connected, the first attached **DirectInput** game controller
     is polled instead (via `SharpDX.DirectInput`) — this is how Windows exposes PS4/PS5 pads,
     since they don't identify as XInput devices. Button indices (Confirm/Cancel/Refresh) are
     configurable via `DirectInputButtonMappings` in case a pad numbers its buttons
     differently than the DualSense default.
   - Verified end-to-end with a real DualSense controller: D-pad + left-stick navigation and
     Cross-to-confirm launched Ratchet & Clank in PCSX2 over the network share.
3. Pressing **A**/Cross (or Enter) on a game calls `GET api/v1/games/{id}/launch` to get the
   `LaunchIntentDto` (title/platform/`networkPath`), resolves the emulator for that platform
   from `EmulatorMappings` in `appsettings.json`, and starts it with `Process.Start`. If that
   emulator's mapping has `HideWindow: true`, its `HideWindowArgument` (default `-nogui`,
   PCSX2's flag) is appended so only the game itself shows, with no menu/UI window flashing
   up first.
4. The picker waits for the emulator process to exit, then re-shows the grid.
5. **Start**/Options (or F5) refreshes the library from the API. **B**/Circle/Escape
   goes back out of a folder (or dismisses an error).
6. **While a game is running**, holding **Back+Start** (Xbox) / **Share+Options**
   (DualShock/DualSense) together for `QuitHoldMilliseconds` (default 1.5s) closes the
   emulator and returns to the picker — handy for switching games without touching a
   keyboard or mouse. By default this requests a graceful close first (`CloseMainWindow`),
   then force-kills the process tree after a 5s grace period if the emulator doesn't
   respond — but if the emulator's `EmulatorMappings` entry has `ForceKillOnQuit: true`
   (see below), it's killed immediately instead. For testing without a controller, the
   **Q** key does the same immediately (no hold required).
7. If a game has a `SaveStats` entry configured (keyed by title), the picker reads its PS2
   memory card save file directly and shows a "Bolts / Playtime" line in the hero area
   whenever that game is focused (see **Save-file stats** below).

## Home screen design

The UI follows the "10-foot" patterns of Google TV, tvOS and Xbox, so web apps and games feel like native apps.

- **Canvas and safe area.** Everything is laid out on a 1920×1080 canvas inside a `Viewbox`, with a 5% safe-area margin, so it scales crisply to any TV resolution and never runs under a TV's overscan.
- **Hero instead of captions.** Artwork tiles carry no captions; the focused tile's title, metadata, synopsis and save stats are shown large above the row (`HeroDetails`), with its artwork faded in behind them (`HeroBackdrop`).
  The one exception is tiles that share their art, such as a show's seasons, which caption it over a bottom scrim (`CardArt.CaptionsArt`) so they can be told apart.
  The backdrop also darkens under the header, so the header stays legible over bright art.
- **Focus.** The focused tile scales up with a white focus ring and shadow, and the others dim; motion uses shared durations and easing (`Motion` in code, `FocusDuration`/`StandardEase` in `App.xaml`).
- **Spatial navigation.** The D-pad moves to the nearest tile on screen in that direction (`SpatialNavigator`), and the focused row scrolls smoothly to the top.
- **Artwork.** Tile art loads in the background and fades in (`ArtImage`); until it has loaded, the tile shows its brand colour, icon and title. Prime Video artwork is requested at display size from Amazon's image CDN (`PrimeVideoArtwork`) rather than as multi-megabyte originals, and Disney+ and HBO Max artwork likewise from their image services (`DisneyPlusArtwork`, `HboMaxArtwork`).
- **Launch splash.** Opening anything shows the tile's art large and centred in its brand colour (`SplashOverlay`), and the splash stays up behind the game or app for the whole session. Errors and the update prompt use the same overlay, so every modal state shares one look.
- **Button prompts.** The hint bar shows real controller glyphs for the connected pad (Xbox or PlayStation) and only lists actions that apply to the focused tile (`HomeHints`).
- **Settings.** The last Home tile opens Settings (`SettingsMenu`), where each setting is a folder whose subtitle shows its current value.
  Choosing an option saves it at once to `%LOCALAPPDATA%\HoloNet\TvLauncher\settings.json` (`LauncherSettingsStore`), and the option in use shows a check mark.
- **Rearranging Home.** Press **X**/Square (or Delete) on a Home tile to pick it up; arrows appear around it.
  The D-pad moves it to the neighbouring spot, Confirm puts it down and saves the order to `settings.json`, and Cancel puts everything back.
  Settings stays last and can't be moved.
  Tiles are matched by key (`games`, `streaming:<provider>`, `shortcut:<title>`), so a newly added tile appears after the arranged ones (`HomeOrder`).
- **Screensaver.** After `ScreensaverIdleMinutes` without input, the launcher shows the screensaver chosen under Settings → Screensaver (`ScreensaverController`).
  Each style is an `IScreensaverScene`: *Ambient clock* (the default) shows a large dimmed clock, the date and the weather, and moves to a new spot every minute; *Bouncing logo* drifts a dim HoloNet logo around the screen.
  Choosing a style shows a preview, and any button dismisses it.
  The weather comes from [Open-Meteo](https://open-meteo.com/), which needs no API key, for this PC's own location from the Windows location service (`HomeLocationService`).
  The location is rounded to about a kilometre before it leaves the PC, and the place name is looked up with BigDataCloud's free reverse geocoding.
  To pin another place, set `WeatherLatitude`/`WeatherLongitude` (and optionally `WeatherLocationName`); with neither available, the weather is hidden.

## Playback controls

While a Prime Video, Disney+ or HBO Max title plays, the launcher draws its own TV-style playback controls over the fullscreen browser (`PlaybackOverlayController`, `PlaybackOverlayWindow`).
They are a transparent, click-through, always-on-top WPF window rather than a browser extension, so they look and behave the same as the rest of the launcher.
Playback state and commands go through Windows' system media sessions (`MediaSessionService`, the same feed as the volume flyout), so the controls work for any site the browser plays.
Some sites (Disney+) publish only whether they're playing, with no timeline.
For those the controls hide the timeline, and skipping presses the player's own arrow-key shortcuts in the browser instead (`PlayerKeys`).

| Button | Action |
|---|---|
| Any button | Show the controls |
| A / ✕ | Play or pause |
| D-pad left/right | Skip 10 seconds per press; presses add up and are applied 0.7 s after the last one (without a timeline, each press skips straight away) |
| B / ○ | Cancel a pending skip, otherwise hide the controls |
| Hold Back+Start / Share+Options | Pause and return home |

The controls fade away 4 seconds after the last press while playing, and stay up while paused.
Returning home leaves the browser paused behind the launcher, so choosing the title again resumes the same tab.
Closing the browser also returns home.

Known limitations:

- The site's own subtitles render underneath the controls' scrim while they are shown.
- If the site autoplays the next episode, the controls keep showing the previous episode's title.

## Streaming folders (Prime Video, Disney+ and HBO Max)

`PrimeVideoEnabled`, `DisneyPlusEnabled` and `HboMaxEnabled` show the "Prime Video", "Disney+" and "HBO Max" folders on the home screen.
Which titles they list is kept in `HoloNet.Video`'s library (`VideoLibraryApiBaseUrl`), and each title's seasons, episodes and progress come from its catalog (`VideoCatalogApiBaseUrl`).
The library is managed from the launcher itself:

- **Add.** Each folder ends with an "Add a title" tile.
  It asks for a link to a show or film, copied from the Prime Video, Disney+ or HBO Max website, and fills it in from the clipboard when it holds one.
  The service is worked out from the link, so a Disney+ link added from the Prime Video folder lands in the Disney+ folder.
  New titles are listed first.
- **Remove.** Press **X**/Square (or Delete) on a title in a service's folder, then confirm.

Episodes and films from every service play in your real, signed-in Firefox through `HoloNet.PrimePlayer` (see its README), so they use the profile you're signed in with there.

## Save-file stats (PS2 memory card reading)

For games with a `SaveStats` entry configured, the picker reads a handful of bytes directly
out of the emulator's memory card save file and shows them in the hero area (currency +
playtime) for whichever game is currently selected — this is deliberately shown as an
always-visible panel tied to gamepad selection, not a mouse-hover tooltip, since this app
has no mouse-driven navigation.

This only reflects the state of the **last save**, not live in-game progress — there's no
way to read stats from a running emulator process, only from what's already been written to
disk. It also only works for games that have been manually reverse-engineered: PS2 save
files have no common/generic format, so every game (and often every region/version of the
same game) needs its own byte offsets found by hand (typically: dump a save, compare known
in-game values against the raw bytes to find where they live, as was done for the shipped
Ratchet & Clank example).

```jsonc
"SaveStats": {
  "Ratchet and Clank": {
    "MemoryCardPath": "C:\\Users\\Goa\\Documents\\PCSX2\\memcards\\Mcd001.ps2",
    "SaveDirectoryName": "BESCES-50916RATCHET",
    "SaveFileName": "save0.bin",
    "CurrencyOffset": 36,
    "CurrencyLabel": "Bolts",
    "PlaytimeFramesOffset": 60,
    "PlaytimeFrameRate": 60.0
  }
}
```

- The dictionary key must exactly match the game's **title** as returned by the Games API
  (case-insensitive) — not its `Id`, since a game's `Id` is a Base64Url-encoded absolute
  file path (per HoloNet's file-identity convention) and isn't stable across machines.
- `MemoryCardPath` is the PCSX2 memory card image file (e.g. `Mcd001.ps2`) to read from.
- `SaveDirectoryName` and `SaveFileName` identify the save directory and the individual
  save-slot file within it on the memory card — list them with a memory-card browser tool
  such as [mymc+](https://github.com/Zueuk/mymc-plus) if you need to find them for a new
  game.
- `CurrencyOffset`/`CurrencyLabel` and `PlaytimeFramesOffset`/`PlaytimeFrameRate` are the
  reverse-engineered byte offsets within that save file — all values are little-endian
  32-bit integers. Playtime is stored as a raw frame count; divide by the game's frame rate
  (`PlaytimeFrameRate`, typically 50 for PAL or 60 for NTSC — Ratchet & Clank's counter runs
  at 60fps even on this PAL disc, confirmed empirically) to get seconds. Either field can be
  omitted (leave the offset unset) if only one stat is known/wanted.
- The memory card image is parsed directly in C# (`Ps2MemoryCardReader`) — a minimal
  read-only implementation of the PS2 memory card FAT-like file system (superblock,
  indirect FAT, directory entries) just deep enough to locate one named file inside one
  named save directory and return its bytes. It intentionally skips ECC verification/repair
  and all write support, since this is read-only and used only for display.

## Configuration (`appsettings.json`)

```jsonc
"TvLauncher": {
  "GamesApiBaseUrl": "http://games.goa.no/api/v1/games",
  "GamepadPollIntervalMs": 100,
  "GamepadStickDeadzone": 0.5,
  "QuitHoldMilliseconds": 1500,
  "DirectInputButtonMappings": {
    "Confirm": 1,
    "Cancel": 2,
    "Refresh": 9,
    "Share": 8,
    "Secondary": 3
  },
  "EmulatorMappings": {
    "PS2": {
      "ExecutablePath": "C:\\Program Files\\PCSX2\\pcsx2-qt.exe",
      "ArgumentsTemplate": "-fastboot -fullscreen -- \"{NetworkPath}\"",
      "HideWindow": true,
      "ForceKillOnQuit": false
    }
  }
}
```

`Platform` must match the string returned by the Games API exactly (case-sensitive
dictionary key). `{NetworkPath}` in `ArgumentsTemplate` is replaced with the game's SMB
network path before launching — the TV PC must already have access to that share (same as
the manual PCSX2 test described in `BACKLOG.md`).

`HideWindow` (per-emulator, defaults to `false`) appends `HideWindowArgument` (default
`-nogui`) to the launch arguments, so the emulator's own menu/UI window never appears — only
the game itself shows fullscreen. Turn it off if you'd rather see the emulator's normal
window/UI.

`ForceKillOnQuit` (per-emulator, defaults to `false`) controls how the quit combo closes
that emulator:
- `false` (default) — graceful close (`CloseMainWindow`), falling back to a force-kill only
  if it doesn't respond within 5s. Lets a normally-windowed emulator show its own save/exit
  prompts. Works with `HideWindow: true` too — PCSX2 will still show its (hidden window's)
  "are you sure?" confirmation dialog, which you can decline/confirm as normal since the
  dialog itself isn't hidden, just its parent window.
- `true` — kills the process immediately, no graceful close attempt at all. Useful if you'd
  rather skip that confirmation dialog entirely and always quit instantly.

### Shortcut tiles and kiosk web apps

`Shortcuts` adds non-game tiles that open a URL.
By default the URL opens in the system default browser.
With `"Kiosk": true`, it opens as a fullscreen app in Firefox kiosk mode with uBlock Origin, and the quit combo returns to the picker.
See [`Firefox/README.md`](Firefox/README.md) for the one-time setup, plus the optional `FirefoxPath` and `KioskFirefoxProfilePath` settings.

Each shortcut can also set how its tile looks:

```jsonc
{
  "Title": "YouTube", "Url": "https://www.youtube.com", "Kiosk": true,
  "Icon": "Play", "AccentColor": "#FF0033",
  "Description": "YouTube in fullscreen, with uBlock Origin blocking the ads."
}
```

- `Icon` is one of `Game`, `Video`, `Tv`, `Photo`, `Play`, `Folder`, `Key`, `Globe` or `Music` (see `TileIcons`); without one, the tile shows the title's initials.
- `AccentColor` is the tile's brand colour as `#RRGGBB`, also used for the backdrop glow and the launch splash.
- `Description` is the synopsis shown in the hero area when the tile is focused.

## PS4/PS5 controller over Bluetooth + PCSX2

If a DualShock/DualSense pad is connected over **Bluetooth**, PCSX2's default input backend
(SDL, using HIDAPI) opens the Bluetooth HID device **exclusively** while running — Windows
only allows one exclusive reader of a Bluetooth HID controller at a time. This means
TvLauncher's own DirectInput polling silently sees nothing at all while a game is running
(not just the quit-combo — no button presses reach TvLauncher whatsoever), even though
navigation worked fine at the picker menu a moment earlier. **Wired/USB controllers don't
have this problem** — USB HID allows multiple simultaneous non-exclusive readers, so both
PCSX2 and TvLauncher can read the same physical pad at once.

**Fixed — see [`docs/tvlauncher-dualsense-bluetooth-fix.md`](../docs/tvlauncher-dualsense-bluetooth-fix.md)
for the full investigation.** In short: once PCSX2/SDL touches a Bluetooth DualSense (to use
its motion/adaptive-trigger/touchpad features), the pad switches from its "basic" HID input
report (ID `1`, which Windows' generic HID parser reads fine) to a Bluetooth-only "extended"
report (ID `0x31`, 78 bytes) that Windows' generic parser has **no** usage definitions for at
all — and the pad stays in that mode, even after the game closes, until it's fully
power-cycled. This is why the controller previously went completely unresponsive in
TvLauncher (not just the quit combo — all navigation) immediately after quitting a game,
recoverable only by turning the pad off and back on.

`RawInputGamepadReader` (a Raw Input-based reader that runs alongside DirectInput, its
results OR'd together each poll) now falls back to a manually-parsed, CRC-32-validated,
exact vendor/product/report-matched parser (`KnownGamepadReportFormats`) for that specific
report shape whenever the generic parse fails — so the pad keeps working immediately after
quitting, no power-cycle needed. Two earlier attempts at fixing this are kept below for
historical context, since they're what led to correctly diagnosing the real root cause:

- **Attempt 1 — switch PCSX2 to DirectInput for Bluetooth controllers.** Based on the
  (incorrect) theory that SDL was opening the Bluetooth HID device exclusively, blocking
  TvLauncher's own DirectInput polling. Worked once, then failed to detect any input at all
  in a later session (PCSX2's DInput binding page reported "No generic bindings were
  generated for device") — abandoned once it proved unreliable.
- **Attempt 2 — let PCSX2 handle the quit itself via a global hotkey**, sidestepping
  TvLauncher's input entirely by having PCSX2 (Settings → Hotkeys → "Open Pause Menu") detect
  the quit itself via its own already-working SDL input path. A parallel Raw Input-based quit
  detector (`RawInputQuitComboListener`, shipped in `v0.3.0`) was also tried at this point but
  reported no detection at all in live testing, and was archived on the
  `archive/rawinput-quit-combo` branch without being debugged further.

Neither of these actually addressed the real problem: TvLauncher's input wasn't blocked by
exclusivity, it was blind to a specific HID report format the pad switches into. That was
only found by adding the opt-in diagnostic logging described in the fix doc above, which is
what makes the current fix reliable rather than another guess.

## Deploying to the TV PC

1. `dotnet publish HoloNet.TvLauncher -c Release -r win-x64 --self-contained false -o publish`
2. Copy the `publish` folder to the TV PC, edit `appsettings.json` there for the real API
   URL and emulator paths.
3. Confirm the TV PC can already reach the network share manually (map the drive or browse
   to it once) before relying on the launcher.

## Auto-start on boot ("console mode")

Use Windows' own Startup folder — no extra tooling needed:

1. `Win+R` → `shell:startup` to open the current user's Startup folder.
2. Create a shortcut to `HoloNet.TvLauncher.exe` in that folder.
3. (Optional) Configure the TV PC to auto-login on boot (`netplwiz` → uncheck "Users must
   enter a password") so it boots straight into the picker without any manual login step.

For more control (delay until network/share is available, restart-on-crash), use Task
Scheduler instead: trigger "At log on", action = the exe, and check "Restart task if it
fails" under Settings.

## Known limitations / next steps

- Only the first connected controller is polled (XInput checked first, then the first
  DirectInput device) — no simultaneous multi-controller support.
- `DirectInputButtonMappings` defaults (Confirm=1, Cancel=2, Refresh=9, Share=8) match a
  DualSense's Cross/Circle/Options/Share over Bluetooth — a different DirectInput pad may
  number its buttons differently; adjust the mapping if navigation or the quit combo
  doesn't match.
- No visual "quit" overlay/confirmation — the hold-combo closes the emulator directly.
  A rendered overlay was considered, but true exclusive-fullscreen emulator windows can
  block any other window (including WPF) from drawing on top, the same limitation
  Steam/Discord overlays solve via GPU-level injection — out of scope for now. The
  hold-to-quit design avoids this entirely and works regardless of fullscreen mode.
- No cover art yet — cards show a title-initials placeholder
  (see `photo-thumbnails`/`games-cover-art` backlog items for a similar pattern once game
  cover art support is added).
- No authentication — relies on the LAN-only nature of `*.goa.no` services, consistent with
  the rest of HoloNet.
- **Raw Input fallback now active.** `RawInputGamepadReader` reads gamepad state directly via
  `RegisterRawInputDevices`/`WM_INPUT`, running alongside DirectInput, specifically to keep
  working through a Bluetooth DualSense's HID report-format switch that leaves DirectInput
  and Windows' own generic HID parsing unable to read the pad at all — see
  [`docs/tvlauncher-dualsense-bluetooth-fix.md`](../docs/tvlauncher-dualsense-bluetooth-fix.md)
  for the full root cause and fix. An earlier, quit-combo-only version of this idea
  (`RawInputQuitComboListener`, `v0.3.0`) reported no detection at all in testing and was
  archived on `archive/rawinput-quit-combo` — the current, more general reader superseded it
  once the real root cause was understood.
