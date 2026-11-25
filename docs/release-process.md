# Release process

HoloNet ships two kinds of artifacts from one release: versioned Docker images for the three
deployed web services (Video, Photos, Games — Portal is not currently deployed; see its own
notes), pulled by `docker-compose.yml` on the home server, and a Velopack-packaged TvLauncher
installer/updater attached to the GitHub Release.

## Branch convention

1. When `main` is in a state you're happy shipping, cut a release branch:
   ```
   git checkout -b release/1.4.0
   git push -u origin release/1.4.0
   ```
   This freezes a stable point to test against (deploy to the server, try TvLauncher on the
   TV PC) while `main` keeps moving with new work.
2. If testing surfaces a bug, fix it on the release branch directly (or cherry-pick from
   `main`) and push — `ci.yml` builds `release/**` branches the same as `main`, so you get the
   same build feedback.
3. Once the release branch is verified, tag it and push the tag:
   ```
   git tag v1.4.0
   git push origin v1.4.0
   ```
   The tag — not the branch name — is what `release.yml` reacts to, so tagging is what
   actually triggers building/publishing the images, packing the TvLauncher Velopack release,
   and creating the GitHub Release.

   Or, without pushing a tag yourself (e.g. from your phone, or from a Claude Code cloud
   session, whose git access can only push its own branch): run the **Release** workflow
   manually on the release branch (or `main`) with the version, e.g. `1.4.0` — from the
   Actions tab, or `gh workflow run release.yml --ref release/1.4.0 -f version=1.4.0`. It
   builds and publishes exactly the same, and creates the `v1.4.0` tag on that commit when it
   creates the GitHub Release, so the tag only exists once the release does. It refuses a
   version that isn't `X.Y.Z` (optionally `-suffix`), a tag that already exists, or a branch
   other than `main` and `release/*`.
4. Merge the release branch back into `main` (fast-forward or a merge commit) so any
   release-branch-only fixes aren't lost, then delete the release branch.

## Deploying a release

- **Server (Portal/Video/Photos/Games):**
  ```
  IMAGE_TAG=v1.4.0 docker compose pull
  IMAGE_TAG=v1.4.0 docker compose up -d
  ```
  Rolling back is the same command with the previous tag.
- **TvLauncher:**
  - **First time on a TV PC:** download `HoloNetTvLauncher-win-Setup.exe` from the release's
    assets on GitHub and run it once. This installs to `%LocalAppData%\HoloNetTvLauncher\` and
    creates a shortcut — after this, do **not** replace it with a plain zip/portable copy, since
    auto-update only works for an installed copy (`UpdateManager.IsInstalled`).
  - **Every release after that:** nothing to do manually. TvLauncher checks GitHub Releases in
    the background at startup, every few hours and when the screensaver comes on, downloads a
    newer version silently, and shows an "Update ready" notice in its top bar — press Menu to
    review and confirm, or ignore it and keep playing (see
    `HoloNet.TvLauncher/Services/AppUpdateTracker.cs`). To get a release onto the TV straight
    away, use Settings → Check for updates. Installing shows the launcher's own install screen;
    the launcher closes for a few seconds while Velopack swaps the files, then comes back as the
    new version.
  - Rolling back TvLauncher: run an older release's `-Setup.exe` again, or use
    `vpk download github` + `vpk pack`/republish locally if you need to force a downgrade.

## Local development

Use the override file to build from source instead of pulling from ghcr.io:
```
docker compose -f docker-compose.yml -f docker-compose.override.yml up --build
```
