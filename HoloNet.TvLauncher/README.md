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
   currently only dismisses error overlays.
6. **While a game is running**, holding **Back+Start** (Xbox) / **Share+Options**
   (DualShock/DualSense) together for `QuitHoldMilliseconds` (default 1.5s) closes the
   emulator and returns to the picker — handy for switching games without touching a
   keyboard or mouse. By default this requests a graceful close first (`CloseMainWindow`),
   then force-kills the process tree after a 5s grace period if the emulator doesn't
   respond — but if the emulator's `EmulatorMappings` entry has `ForceKillOnQuit: true`
   (see below), it's killed immediately instead. For testing without a controller, the
   **Q** key does the same immediately (no hold required).
7. If a game has a `SaveStats` entry configured (keyed by title), the picker reads its PS2
   memory card save file directly and shows a small "Bolts / Playtime" info panel in the
   bottom-right corner whenever that game is the currently-selected card (see
   **Save-file stats** below).

## Save-file stats (PS2 memory card reading)

For games with a `SaveStats` entry configured, the picker reads a handful of bytes directly
out of the emulator's memory card save file and shows them as an info panel (currency +
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
    "Share": 8
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

## PS4/PS5 controller over Bluetooth + PCSX2

If a DualShock/DualSense pad is connected over **Bluetooth**, PCSX2's default input backend
(SDL, using HIDAPI) opens the Bluetooth HID device **exclusively** while running — Windows
only allows one exclusive reader of a Bluetooth HID controller at a time. This means
TvLauncher's own DirectInput polling silently sees nothing at all while a game is running
(not just the quit-combo — no button presses reach TvLauncher whatsoever), even though
navigation worked fine at the picker menu a moment earlier. **Wired/USB controllers don't
have this problem** — USB HID allows multiple simultaneous non-exclusive readers, so both
PCSX2 and TvLauncher can read the same physical pad at once.

**Fix: switch PCSX2 to DirectInput instead of SDL for Bluetooth controllers.**

1. In PCSX2, go to **Settings → Controllers**.
2. Under **Input Sources**, uncheck **SDL** and check **DInput** (DirectInput)/RawInput
   (naming varies slightly by PCSX2 version).
3. Go to the controller port binding page and re-bind your buttons — they'll now appear
   under the DInput device instead of the SDL one.
4. Re-bind any hotkeys (e.g. an in-emulator Exit/Shutdown binding) under DInput too.

With DInput enabled, PCSX2 shares the Bluetooth controller non-exclusively, the same as a
wired pad — TvLauncher's quit-combo and general input keep working while a game is running.
This was confirmed working end-to-end once on a Bluetooth-paired DualSense, but turned out
**not to be reliable**: on a later session, PCSX2's DInput binding page failed to detect any
input from the same DualSense at all (auto-mapping reported "No generic bindings were
generated for device", and manual rebind capture saw nothing either), even though the
controller worked fine in TvLauncher and over USB. Switching back to SDL (PCSX2's default)
restores in-game input immediately, at the cost of TvLauncher going blind again during
gameplay. Root cause is believed to be a DirectInput/legacy-joystick compatibility gap with
the DualSense's Bluetooth report format, not the exclusivity issue this fix originally
targeted — see the Raw Input backlog item below for the real long-term fix.



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
- **Raw Input quit-combo fallback (implemented).** The quit combo (Options+Share /
  Back+Start) is now additionally detected via the Windows **Raw Input API**
  (`RegisterRawInputDevices`/`WM_INPUT`, see `RawInputQuitComboListener`), running alongside
  the existing XInput/DirectInput polling rather than replacing it — wired controllers are
  unaffected either way. This exists specifically because DirectInput can go completely
  silent for a Bluetooth-connected DualSense while PCSX2 is running (see "PS4/PS5
  controller over Bluetooth + PCSX2" above), regardless of which PCSX2 input source is
  selected; Raw Input reads HID reports through a different Windows subsystem (the same one
  SDL itself uses internally for exactly this kind of multi-consumer scenario) and keeps
  working even when DirectInput can't see the pad at all. Deliberately scoped to *only* the
  quit combo, not full menu navigation, to minimize new surface area — **not yet verified
  live on Bluetooth hardware**; needs testing with a Bluetooth DualSense + PCSX2 running
  before being considered confirmed-working (see the earlier DInput fix, which also looked
  solved before turning out to be unreliable).
