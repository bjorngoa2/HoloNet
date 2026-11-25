# HoloNet.PrimeAuth

A small standalone tool that opens a real, visible browser window so you can log into Prime
Video normally, then captures the resulting session cookie and pushes it into a running
`HoloNet.Video` instance - replacing the "manually copy the Cookie header out of DevTools"
workflow with a one-command refresh.

## Why this exists

`HoloNet.Video`'s Prime Video catalog provider (see `HoloNet.Video/Providers/PrimeVideo`) needs
a live, logged-in session cookie to call Prime's (undocumented) `detail/{id}` endpoint. There is
no official third-party auth flow for Prime Video, and the cookie has no refresh token - it just
expires unpredictably and has to be replaced.

`HoloNet.Video` itself runs **headless** in a Docker container on the home server, so it can
never show you a login window. This tool is meant to be run instead, on a machine that **does**
have a display - typically the TV PC (where `HoloNet.TvLauncher` also runs), or your own PC.

## What it does (and deliberately does not do)

1. Launches a real Chromium browser window (via Playwright) and navigates to
   `https://www.primevideo.com`.
2. **You** log in normally in that window - username/password, CAPTCHA, 2FA, whatever Amazon
   asks for. This tool never sees or stores your password, and never attempts to script/automate
   the login form itself.
3. Once you're logged in and can see your Prime Video home page, you come back to the console
   window and press Enter.
4. The tool reads the cookies your own browser session now holds for `primevideo.com` (via
   Playwright's `BrowserContext.CookiesAsync`, filtered to exactly what a real request to that
   origin would send - the same shape as manually copying the `Cookie` header from DevTools) and
   sends them to `HoloNet.Video`'s `PUT api/v1/admin/primevideo/session-cookie` endpoint.
5. `HoloNet.Video` persists the cookie to `HoloNet.Video/data/prime-session.json` (already
   gitignored) so it survives a container restart, and starts using it immediately - no restart
   needed.

It never touches Prime's playback/DRM endpoints (`GetVodPlaybackResources`,
`GetWidevineLicense`) - this tool's only job is capturing and relaying the same cookie you could
otherwise copy-paste by hand.

## Setup (once per machine)

Playwright needs its browser binaries installed once:

```powershell
dotnet build HoloNet.PrimeAuth
pwsh HoloNet.PrimeAuth\bin\Debug\net10.0\playwright.ps1 install chromium
```

(If you don't have `pwsh`, PowerShell 5's `powershell` works too - the script is plain
PowerShell.)

## Running it

```powershell
dotnet run --project HoloNet.PrimeAuth -- http://videos.goa.no
```

Or, against a locally-running dev instance of `HoloNet.Video`:

```powershell
dotnet run --project HoloNet.PrimeAuth -- http://localhost:5044
```

A Chromium window opens - log in, then press Enter in the console. You should see
`Done - HoloNet.Video's Prime Video session has been refreshed.` on success.

## Triggering it from HoloNet.TvLauncher

The picker's "Prime Video" folder includes a "Refresh Prime Login" tile that launches this tool
as a child process (same pattern as launching an emulator) and waits for it to exit - see
`HoloNet.TvLauncher/Services/PrimeAuthLauncher.cs`. Configure
`TvLauncher:PrimeAuthToolPath` in `appsettings.json` to point at this tool's published exe.
