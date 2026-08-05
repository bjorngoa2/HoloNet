# HoloNet — Prioritized Backlog

## Codebase Snapshot

| Service | Status |
|---|---|
| `HoloNet.Video` | ✅ API complete (list, get, stream) — no CORS, no Docker |
| `HoloNet.Photos` | ✅ API complete (list, get, image) — wrong content-type bug, no Docker |
| `HoloNet.Games` | ✅ API complete (list, get) — no launch endpoint, no Docker |
| `HoloNet.Portal` | ❌ Empty boilerplate — no dashboard, no frontend |
| `HoloNet.Shared` | ✅ FileId, MediaDirectory, ServiceBaseUrl helpers done |
| Docker / Infra | ❌ No Dockerfiles, no docker-compose.yml, no Nginx config |

---

## 🔴 P1 — Critical (must-have before services are usable)

- [x] **`cors-all`** — Add CORS middleware to all 3 services
  - Browser will block portal → service API calls without this
- [x] **`photos-content-type`** — Fix Photos content-type (hardcoded `image/png` for all images)
  - JPEGs/WebPs render broken in browsers
- [x] **`portal-dashboard`** — React + TypeScript (Vite) SPA in `HoloNet.Portal/ClientApp/`; builds to `wwwroot/`; served via `UseStaticFiles()`; service cards for Videos, Photos, Games
- [x] **`docker-all`** — Write Dockerfiles + `docker-compose.yml` for all services + Nginx
  - Nothing runs on the server without this

---

## 🟡 P2 — High (correctness & maintainability)

- [x] **`async-io`** — Make file I/O truly async in Video & Photos services
  - Directory scans offloaded to `Task.Run` background threads so they don't block request threads
- [x] **`problem-details`** — Return `ProblemDetails` on errors (RFC 7807)
  - `AddProblemDetails()` + `UseExceptionHandler()`/`UseStatusCodePages()` + `Results.Problem(...)` on bad input
- [x] **`input-validation`** — Validate `id` params early in all endpoints
  - All `{id}` endpoints check `string.IsNullOrWhiteSpace(id)` before hitting file I/O
- [x] **`health-checks-real`** — Make health checks verify the data directory is accessible
  - `MediaDirectoryHealthCheck` (shared) checks existence/readability of the configured media path
- [x] **`nginx-config`** — Write Nginx reverse proxy config routing `*.goa.no` to containers
  - `nginx/nginx.conf` routes portal/videos/photos/games subdomains, with range-friendly settings for video

---

## 🟢 P3 — Medium (feature completeness)

- [x] **`games-launch-endpoint`** — Add `GET api/v1/games/{id}/launch` — returns launch-intent for TV PC
  - Core feature from PLAN.md — lets the TV PC know which game to open in the emulator
- [x] **`games-search-filter`** — Add query params to `GET api/v1/games` (platform, year, genre)
  - Useful once the game library grows
- [x] **`video-content-type`** — Detect MIME type per extension in Video stream
  - Maps `.mp4`/`.mkv`/`.avi`/`.mov` to correct content-type, falls back to `application/octet-stream`
- [x] **`games-file-size`** — Add `FileSizeBytes` to `GameDto`
  - Parity with Video/Photos DTOs
- [x] **`photos-health-check`** — Add `AddHealthChecks()` + `MapHealthChecks()` to Photos service
  - Photos now has `MediaDirectoryHealthCheck` wired up like Video/Games

---

## 🔵 P4 — Low / Future

- [ ] **`photo-thumbnails`** — Thumbnail generation with `ImageSharp` or `SkiaSharp`
  - Add `GET api/v1/photos/{id}/thumbnail` — browsing many photos is slow at full resolution
- [ ] **`games-cover-art`** — Manual cover art support in game `.json` metadata sidecar
  - Add optional `CoverArtUrl` field to `GameMetadata` and `GameDto`
- [ ] **`gitea`** — Add Gitea container to `docker-compose.yml`
  - Self-hosted Git — quick win once infra is up; route `git.goa.no` via Nginx
- [ ] **`vpn-tailscale`** — Set up Tailscale VPN on the server
  - Remote access to all services from outside the LAN
- [ ] **`ai-integration`** — Semantic search, Ollama local LLM, Semantic Kernel
  - Phase 7 — long-term

---

## Suggested Execution Order

1. `cors-all` + `photos-content-type` — fix what's broken in existing code
2. `async-io` + `problem-details` + `input-validation` — code quality before shipping
3. `portal-dashboard` — visible milestone, something to show
4. `docker-all` + `nginx-config` — deploy to the server
5. `health-checks-real` — production readiness
6. `games-launch-endpoint` + `games-search-filter` — Games feature completeness
7. P4 items as desired
