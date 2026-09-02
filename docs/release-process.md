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
    the background on each launch, downloads a newer version silently, and shows a "🔔 Update
    ready" hint in its status line — press Start to review and confirm, or ignore it and keep
    playing (see `HoloNet.TvLauncher/Services/AppUpdateService.cs`).
  - Rolling back TvLauncher: run an older release's `-Setup.exe` again, or use
    `vpk download github` + `vpk pack`/republish locally if you need to force a downgrade.

## Local development

Use the override file to build from source instead of pulling from ghcr.io:
```
docker compose -f docker-compose.yml -f docker-compose.override.yml up --build
```
