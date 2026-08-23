# Release process

HoloNet ships two kinds of artifacts from one release: versioned Docker images for the four
web services (Portal, Video, Photos, Games), pulled by `docker-compose.yml` on the home
server, and a self-contained TvLauncher build attached as a zip to the GitHub Release.

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
   actually triggers building/publishing the images and the TvLauncher zip and creating the
   GitHub Release.
4. Merge the release branch back into `main` (fast-forward or a merge commit) so any
   release-branch-only fixes aren't lost, then delete the release branch.

## Deploying a release

- **Server (Portal/Video/Photos/Games):**
  ```
  IMAGE_TAG=v1.4.0 docker compose pull
  IMAGE_TAG=v1.4.0 docker compose up -d
  ```
  Rolling back is the same command with the previous tag.
- **TvLauncher:** download `HoloNet.TvLauncher-v1.4.0.zip` from the release's assets on
  GitHub, extract, run.

## Local development

Use the override file to build from source instead of pulling from ghcr.io:
```
docker compose -f docker-compose.yml -f docker-compose.override.yml up --build
```
