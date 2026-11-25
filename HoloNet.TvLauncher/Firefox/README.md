# Firefox kiosk apps with uBlock Origin

TvLauncher shortcut tiles with `"Kiosk": true` open as fullscreen "apps": Firefox in kiosk mode (no tabs or address bar), using a dedicated profile.
Hold the quit combo (Share+Options / Back+Start) to close the app and return to the picker.

Firefox is used because it is the only mainstream browser that still runs the full uBlock Origin.
Chrome, Edge, and WebView2 only support the reduced "uBlock Origin Lite".

## One-time setup on the TV PC

1. Install Firefox.
2. From an elevated PowerShell, run `.\Install-FirefoxPolicies.ps1` in this folder.
   It copies `policies.json` into Firefox's `distribution` folder, which force-installs uBlock Origin into every profile and skips first-run/default-browser prompts.
3. Add a kiosk tile to `appsettings.json`:

   ```json
   "Shortcuts": [
     { "Title": "YouTube", "Url": "https://www.youtube.com", "Kiosk": true }
   ]
   ```

4. Launch the tile once with a keyboard attached and log into the site.
   The kiosk profile (`%LOCALAPPDATA%\HoloNet\TvLauncher\FirefoxKioskProfile` by default) is separate from your everyday Firefox profile, so logins are not shared.

## Verifying

Open `about:policies` in Firefox to confirm the policies are active, and `about:addons` to confirm uBlock Origin is installed.

## Limitations

- Ads stitched into the video stream itself (as Prime Video does) cannot reliably be blocked by any browser extension.
- DRM-protected streaming sites usually cap Firefox at 1080p or lower.
