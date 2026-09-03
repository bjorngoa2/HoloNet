# TvLauncher: DualSense-over-Bluetooth goes unresponsive after quitting a game

## The symptom

Playing a PS2 game in PCSX2, launched from TvLauncher, with a DualSense
controller connected over **Bluetooth**.
Quitting the game (either via the quit-combo or manually) returns to
TvLauncher's picker screen as expected.
But the controller no longer does anything: no navigation, no
confirm/cancel, nothing.
Only the keyboard still works.
The only way to recover the controller was to fully power-cycle it (hold
the PS button until it turns off, then press it again to reconnect).

Two things made this hard to pin down:

- The controller kept working perfectly **inside** PCSX2/the game itself,
  the whole time it was "broken" in TvLauncher.
  So the pad itself, its Bluetooth pairing, and Windows' Bluetooth stack
  were all fine — this was specific to how TvLauncher was reading it.
- It didn't happen on every single run.
  Sometimes navigation would keep working fine after quitting.
  This made it look intermittent, when it was actually a deterministic
  state change that just doesn't always get triggered
  (see "Root cause" below).

## Investigation

### Step 1 — Rule out the obvious

The DirectInput polling loop already used by TvLauncher for controller
input was checked first, since that's the most likely place for a "went
blind" bug.
Nothing obviously wrong there — the deadzone/button-mapping logic hadn't
changed.

A previous session in this project had already tried a Raw Input based
"quit combo" listener as a parallel input path
(`RawInputQuitComboListener`, shipped in `v0.3.0`), specifically because
DirectInput was known to sometimes miss Bluetooth DualSense input.
That earlier attempt reported "no detection at all" in live testing and
was abandoned/archived rather than debugged further, in favor of binding
PCSX2's own global hotkey to open its pause menu instead
(see the TvLauncher README's "PS4/PS5 controller over Bluetooth + PCSX2"
section, since superseded by this fix).

### Step 2 — Build a live diagnostic log

Since the bug only reproduces on real hardware over a real Bluetooth
connection, and can't be reproduced by reasoning about the code alone, the
first real step was to add an opt-in debug logger
(`GamepadDebugLog`) and instrument every layer of the input pipeline:

- The raw Windows Raw Input HID report as it arrives
  (`WM_INPUT` → `GetRawInputData`).
- The decoded button/D-pad state after Windows' generic HID parsing
  (`HidP_GetUsages`/`HidP_GetUsageValue`).
- The logical `GamepadButton` events raised from that state.
- The UI's reaction to those events (`HandleButton`/`Move()` in
  `MainWindow.xaml.cs`).

This let the actual failure be traced end-to-end from a live test, instead
of guessing.
A shared SMB folder (`tvtest`) was set up so a self-contained test publish
of TvLauncher could be quickly copied to the TV PC (a laptop, in this
testing setup) and the resulting `gamepad-debug.log` copied back for
analysis after each test.

### Step 3 — Read the log after a reproduction

With that logging in place, a test was run where the controller broke
after quitting a game (no power-cycle).
The log showed exactly what changed, right at the moment the pad "went
dead":

```
report reportId=1  length=10 buttonStatus=0x00110000 povStatus=0x00110000
...  (game launches)  ...
report reportId=49 length=78 buttonStatus=0xC011000A povStatus=0xC011000A
```

`buttonStatus`/`povStatus` are the raw return codes from Windows'
`HidP_GetUsages`/`HidP_GetUsageValue` calls.
`0x00110000` is `HIDP_STATUS_SUCCESS`.
`0xC011000A` is `HIDP_STATUS_INCOMPATIBLE_REPORT_ID` — Windows' own HID
parser saying it has no idea how to interpret this specific report.

The report's **HID report ID** (the first byte of every HID input report,
identifying which of a device's several possible report "shapes" this
particular packet is) had changed from `1` to `49` (`0x31`), and its
length had changed from 10 bytes to 78 bytes.
Once that happened, it stayed that way — even long after the game was
closed — which explains why only a full Bluetooth power-cycle ever
recovered it: that's the only thing that resets the pad back to sending
report ID `1` again.

### Step 4 — Understand why report ID 49 breaks Windows' parser

The DualSense (like most modern HID gamepads) can send more than one input
report "shape", each identified by that leading report-ID byte, each
described separately in the device's HID report descriptor (a
machine-readable spec every HID device advertises describing its report
layouts, used by generic OS-level tooling like `HidP_*` to parse any
device without needing per-device code).

Sony's Bluetooth DualSense normally sends a **basic** report (ID `1`) that
Windows can parse generically just fine — this is what TvLauncher reads
successfully before a game runs.
But as soon as something actually talks to the pad using Sony's fuller
feature set (motion/gyro, adaptive triggers, touchpad, LED/rumble — all of
which PCSX2 uses via SDL/hidapi for a more complete emulation experience),
the pad switches into its **extended** report (ID `49`/`0x31`, 78 bytes)
and communicates using that format from then on.

The problem: Sony's declared Bluetooth HID report descriptor only fully
describes the basic report.
The extended report is, from the generic HID parser's point of view, a
vendor-private format with no declared usages at all — which is exactly
why `HidP_GetUsages`/`HidP_GetUsageValue` return
`HIDP_STATUS_INCOMPATIBLE_REPORT_ID` for it, on every single call, forever
(until the pad is reset back to the basic report by a power-cycle).
This isn't a Windows bug or a TvLauncher bug in the traditional sense —
it's a structural gap: applications that want to read the extended report
are expected to already know its byte layout out-of-band, the same way
SDL/hidapi (and therefore PCSX2) already do internally.
DirectInput, and Windows' own generic Raw Input HID parsing, simply have
no path to do that automatically.

### Step 5 — Find the real byte layout

Since Windows offers no generic way to parse report ID `49`, the fix
needed the DualSense's actual byte layout for that report, from a source
that's known to interpret it correctly.
Sony's own official Linux kernel driver source
(`drivers/hid/hid-playstation.c`, fetched directly from the upstream
Linux kernel repository on GitHub) was used as that authoritative
reference, since it's an open-source, actively-maintained,
first-party-adjacent implementation that every DualSense feature (motion,
touchpad, adaptive triggers, LEDs) already works correctly against on
Linux.

From that source:

- The Bluetooth extended report's first 2 bytes are the report ID itself
  plus a Bluetooth sequence/tag byte; the "common" DualSense report layout
  (shared with the USB report) starts right after those 2 bytes.
- Within that common layout, the three button-state bytes land at
  offsets 7-9 — which is offsets **9-11** of the full 78-byte Bluetooth
  report once the leading 2 bytes are accounted for.
- Named bit masks for every button and the D-pad hat switch
  (`DS_BUTTONS0_SQUARE`, `DS_BUTTONS1_L1`, `DS_BUTTONS2_TOUCHPAD`, etc.).
- A CRC-32 checksum in the report's final 4 bytes, seeded with a fixed
  byte (`0xA1`) before the report's own bytes are folded in — Sony's own
  driver refuses to trust a report whose CRC doesn't validate, treating it
  as corrupt/torn.

## The fix

`RawInputGamepadReader` already existed from an earlier session as a
parallel Raw Input based reader (reading `WM_INPUT` messages directly,
independent of DirectInput/SharpDX) — this was extended rather than
replaced.

1. When the generic `HidP_GetUsages`/`HidP_GetUsageValue` calls fail (any
   non-success status, not just this specific one — see "Design notes"
   below), the report is checked against a small registry of known
   manually-parseable report formats
   (`KnownGamepadReportFormats`), matched by **exact** vendor ID, product
   ID, report ID, and report length — so this can never accidentally apply
   to some unrelated device that happens to share a report shape by
   coincidence.
2. If a match is found, its CRC-32 (if the format specifies one) is
   validated first — a report that fails CRC is treated as corrupt and
   discarded rather than acted on, exactly like Sony's own driver does.
3. Only if the CRC passes (or the format has no CRC) is the report handed
   to that format's parser, which fills in the same `bool[] Buttons`/`int
   Pov` shape the generic HID-usage path already produces — using the
   same 0-based button indices, confirmed against live logs
   (Usage ID 2 → Cross/Confirm at index 1, Usage ID 3 → Circle/Cancel at
   index 2, etc.) — so this fallback needed **no changes at all** further
   up the input pipeline (`GamepadInputService`, the quit-combo detection,
   `MainWindow`'s navigation handling).

Vendor/product ID is read once per device via
`GetRawInputDeviceInfo(RIDI_DEVICEINFO)` and cached, the same pattern
already used for caching each device's preparsed HID descriptor.

### Design notes

- **Extensible, not DualSense-specific.** `KnownGamepadReportFormats` is a
  registry (currently one entry), not a single hardcoded method — adding
  support for a different controller/report combination that hits the
  same `HIDP_STATUS_INCOMPATIBLE_REPORT_ID` gap in the future (e.g. a
  Game Boy Advance USB adapter with its own private report, or a DualShock
  4 quirk) means adding one more entry, not writing another one-off
  parser.
- **CRC-32 validation matches upstream Sony driver behavior.** Acting on a
  torn/corrupt Bluetooth packet would be worse than not acting at all —
  it could inject phantom button presses. The CRC check makes this
  fallback exactly as strict as Sony's own reference implementation.
- **Named constants instead of raw hex literals**, mirroring the Linux
  driver's own macro names (`DualSenseButtons.Square`, `.L1`, `.Options`,
  etc.) for self-documenting code, in `KnownGamepadReportFormats.cs`.
- **Kept as a Raw Input fallback, not a DirectInput replacement.**
  `RawInputGamepadReader` runs alongside DirectInput; their button states
  are OR'd together every poll in `GamepadInputService`. Wired pads and
  any pad that never enters this "extended" mode are completely
  unaffected.

## Verification

With opt-in debug logging enabled, a live test was run: launch a game,
play normally (letting PCSX2/SDL switch the pad into its extended report,
confirmed via the log showing `reportId=49`), quit the game via the
quit-combo, and try navigating TvLauncher's picker with the controller —
**without** power-cycling the pad.

The log confirmed the fix working exactly as intended:

- The report format switch to `reportId=49` still happens, same as
  before (this is the pad's own behavior, not something to prevent).
- While the game is running (`isBusy=True`), button/navigation events are
  correctly ignored — this was already true before the fix and is
  unrelated to it.
- The instant `isBusy` flips back to `False` after quitting, navigation
  (`Move`) events start firing immediately from the manually-parsed
  extended report — no power-cycle needed, no delay.

This was confirmed across multiple independent test sessions (including
one with a genuine ~63-minute real gameplay gap, screensaver kicking in
and being dismissed, and one where the user specifically re-tested after
a false-positive "fixed" report turned out to still require a
power-cycle at a different point) before being treated as resolved.

## Files

- `HoloNet.TvLauncher/Services/RawInputGamepadReader.cs` — Raw Input
  reader; owns the "generic parse failed, try a known fallback format"
  decision and the CRC-gated dispatch to it.
- `HoloNet.TvLauncher/Services/KnownGamepadReportFormats.cs` — the
  extensible registry of manually-parseable report formats, the DualSense
  button-bit layout, and the reusable incremental CRC-32 implementation.
- `HoloNet.TvLauncher/Services/GamepadDebugLog.cs` — the opt-in shared
  diagnostic logger that made this whole investigation possible; gated by
  `TvLauncherOptions.EnableGamepadDebugLogging` (default `false`) so it
  has zero cost/noise in normal use, and is reusable for diagnosing future
  controller/emulator additions.
