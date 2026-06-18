# Homelab Personal Infrastructure Platform — Project Plan

## Goal
A self-hosted personal platform running on an always-on home server using Docker Compose.
Services are LAN-only via local DNS (e.g. `portal.home.no`, `videos.home.no`).
Built primarily in ASP.NET Core; fall back to existing apps (Jellyfin, Gitea) if complexity is too high.
Future: AI integration (RAG, search, agents).

---

## Hardware & OS
- **Hardware:** Always-on home server (old PC)
- **OS:** Ubuntu Server 24.04 LTS (headless, best Docker support, 5-year LTS)

---

## Tech Stack
- **Backend:** ASP.NET Core (.NET 8+)
- **Frontend:** Razor Pages or Blazor (within .NET — no separate JS framework needed)
- **Infrastructure:** Docker + Docker Compose
- **Reverse Proxy:** Nginx (or Caddy) — routes subdomains to services
- **Local DNS:** Pi-hole or AdGuard Home (maps `*.home.no` → server IP)
- **Database:** PostgreSQL (general) + SQLite (lightweight services)

---

## Phases

### Phase 1 — Infrastructure Foundation
Get the server, Docker, DNS and reverse proxy working. This is the base everything else runs on.

**Everything runs on the old PC (Ubuntu Server) in Docker:**
```
Your Old PC (Ubuntu Server)
├── Docker
│   ├── AdGuard Home    ← DNS server
│   ├── Nginx           ← reverse proxy
│   ├── HoloNet.Portal
│   ├── HoloNet.Video
│   └── HoloNet.Photos
```

**Request flow:**
```
Your device → asks router for DNS
Router → points to your PC (AdGuard)
AdGuard → "videos.goa.no = 192.168.1.x" (your PC's local IP)
Browser → hits Nginx on your PC
Nginx → forwards to the correct container
```

**Tasks:**
- [ ] In router settings: assign your PC a static local IP via DHCP reservation (e.g. `192.168.1.50`) — do this first, everything depends on a stable IP
- [ ] Install Docker + Docker Compose on Ubuntu
- [ ] Add AdGuard Home to `docker-compose.yml`. Add DNS records: `*.goa.no → 192.168.1.50`
- [ ] In router settings: set DNS server to your PC's IP so all devices use AdGuard
- [ ] Set up Nginx in Docker as a reverse proxy routing subdomains to the correct containers
- [ ] Create the shared Docker network in `docker-compose.yml`

**Result:** You can open `http://portal.goa.no` from any device on your LAN.

---

### Phase 2 — Central Portal (ASP.NET Core)
A simple home dashboard that links to all services. Start small.

**Tasks:**
- [ ] Create an ASP.NET Core web app (Razor Pages or Blazor)
- [ ] Build a simple dashboard UI: cards linking to each service
- [ ] Dockerize it (`Dockerfile` + add to `docker-compose.yml`)
- [ ] Route `portal.home.no` → this container via Nginx

**Tech notes:**
- Use Razor Pages for simplicity; switch to Blazor if you want SPA-like interactions
- Minimal CSS (Tailwind or plain CSS — no heavy framework)

**Result:** `portal.home.no` shows a home screen with links to all your services.

---

### Phase 3 — Video Streaming Service (ASP.NET Core)
Stream videos to browsers and smart TVs.

**Tasks:**
- [ ] Create an ASP.NET Core app that serves video files via HTTP range requests
- [ ] Build a simple library UI: list/browse video files from a mounted folder
- [ ] Implement a video player page (HTML5 `<video>` tag works for most formats)
- [ ] Dockerize with a volume mount pointing to your video storage folder
- [ ] Route `videos.home.no` → this container

**Smart TV support:**
- Most modern smart TVs support DLNA or can open a browser — H.264/MP4 streams just work
- If a TV needs DLNA specifically, add `minidlna` as a separate lightweight Docker container (not worth building from scratch)

**Fallback:** If transcoding becomes an issue (codec support, performance), swap this out for Jellyfin.

---

### Phase 3.5 — Games Library + Living Room Play (ASP.NET Core + PCSX2)
Host and browse your retro game library (starting with PS2), then launch games on a TV-connected PC.

**Recommended architecture (best first version):**
- `HoloNet.Games` API stores and serves game metadata/library state
- NAS stores ISOs/ROMs on mounted folders (no ROM bytes in DB)
- A dedicated living-room PC (or mini-PC) runs emulator(s) like PCSX2
- Controller input stays local to the TV PC (e.g., PS5 DualSense)

**Tasks:**
- [ ] Create `HoloNet.Games` API focused on library indexing and metadata (not emulation)
- [ ] Add folder conventions for platforms (example: `/data/games/ps2`)
- [ ] Implement scanner service for known extensions (`.iso`, `.bin`, `.chd` where applicable)
- [ ] Store metadata in SQLite (`title`, `platform`, `filePath`, `size`, `lastPlayedUtc`, `favorite`)
- [ ] Add endpoints for list/details/search/filter/favorites/recently-played
- [ ] Add optional cover-art metadata provider (manual first, external APIs later)
- [ ] Add a launch-intent endpoint for TV PC handoff (URL/deep-link based)
- [ ] Dockerize `HoloNet.Games` with volume mount to game storage
- [ ] Route `games.home.no` (or `games.goa.no`) to this service via Nginx

**Important boundaries:**
- API is primarily a library/catalog and orchestration layer
- Actual emulation/rendering runs on the TV PC, not in browser via ASP.NET
- Keep all services LAN-only unless behind VPN

**Result:** You can browse games from portal/web and launch PS2 titles (e.g., Ratchet & Clank) on the TV-connected PC with a DualSense controller.

---

### Phase 4 — Photo Storage (ASP.NET Core)
Browse and view photos stored on the server.

**Tasks:**
- [ ] Create an ASP.NET Core app for photo browsing (folder-based or DB-backed)
- [ ] Upload endpoint (drag-and-drop or simple file picker)
- [ ] Thumbnail generation (use `SkiaSharp` or `ImageSharp` NuGet packages)
- [ ] Dockerize with volume mount to photo folder
- [ ] Route `photos.home.no` → this container

**Fallback:** If photo features grow complex (face recognition, albums, sharing), swap in Immich or PhotoPrism.

---

### Phase 5 — Git Hosting
Self-hosted code repositories.

**Tasks:**
- [ ] Evaluate: build a basic Git HTTP backend in ASP.NET Core, OR just use Gitea
- **Recommended:** Use **Gitea** (lightweight, excellent .NET integration, runs in Docker in minutes)
- [ ] Add Gitea to `docker-compose.yml`
- [ ] Route `git.home.no` → Gitea container

**Why not build from scratch:** Git's protocol is complex. Gitea is the right tradeoff here.

---

### Phase 6 — Remote Access (Optional)
Access your homelab from outside your home network.

**Tasks:**
- [ ] Set up **Tailscale** (easiest) or **WireGuard** VPN on the server
- [ ] Access all services via VPN as if you're on the LAN

---

### Phase 7 — AI Integration (Future)
Add AI features to the platform.

**Potential features:**
- Semantic search across your files/photos/notes (RAG + vector DB)
- Local LLM (Ollama) for private chat / developer assistance
- Image tagging / auto-organization for photos
- Code assistant connected to your Gitea repos

**Tech to explore:**
- **Ollama** — run LLMs locally (Llama 3, Mistral, etc.)
- **Qdrant or ChromaDB** — vector database for embeddings
- **Semantic Kernel** — Microsoft's .NET SDK for AI orchestration (great fit since you're on .NET)

---

## Why Separate Projects?

Each service runs in its own Docker container — `videos.goa.no` hits only the Video container, `photos.goa.no` only the Photos container. This means:
- One service crashing doesn't affect the others
- Each can be restarted, updated, or scaled independently
- Each has its own `appsettings.json` and `Dockerfile`

**`HoloNet.Shared`** holds code shared across all services (e.g. a standard `ApiResponse<T>` wrapper) so it isn't copy-pasted into each project.

This is a microservices-lite approach — slightly more setup upfront, but maps naturally to the clear service boundaries (portal, video, photos).

---

## Suggested Folder / Repo Structure

```
homelab/
├── docker-compose.yml          # All services defined here
├── nginx/
│   └── nginx.conf              # Reverse proxy config
├── portal/                     # ASP.NET Core portal app
│   ├── Portal.csproj
│   └── Dockerfile
├── videos/                     # ASP.NET Core video service
│   ├── VideoService.csproj
│   └── Dockerfile
├── photos/                     # ASP.NET Core photo service
│   ├── PhotoService.csproj
│   └── Dockerfile
└── data/                       # Mounted data volumes (gitignored)
    ├── videos/
    ├── photos/
    └── gitea/
```

---

## Recommended Order of Work
1. Phase 1 (infrastructure) — everything depends on this
2. Phase 2 (portal) — gives you a home base
3. Phase 3 or 4 — pick whichever excites you more
4. Phase 5 (Gitea) — quick win, very useful
5. Phase 6 (VPN) — when you want remote access
6. Phase 7 (AI) — long-term fun phase


```
If i hosted my own local picture videos streaming nas dns, could I also have a library of ps2 emulator games and somehow, have a pc connected to the tv playing it?

Yes. What you're describing is essentially a home server/NAS that provides:

Media streaming (photos, videos, music)
Network services (DNS, file sharing, backups, etc.)
A retro gaming library (PS2 ROMs/ISOs)
A living-room gaming PC connected to the TV

There are several ways to set it up.

Option 1: NAS + Dedicated TV Gaming PC (Most Practical)

Server/NAS

Stores movies, photos, backups, and PS2 game ISOs.
Runs services like:
Plex
Jellyfin
Pi-hole (DNS/ad blocking)
SMB/NFS file shares

Living-room PC

Connected to the TV via HDMI.
Runs PCSX2.
Accesses game ISOs directly from the NAS over the network.
Uses wireless controllers.

Advantages:

Simplest.
Best PS2 performance.
Easy to maintain.
Option 2: One Powerful Server Streams Games

A single machine acts as:

NAS
Media server
Gaming machine

Then you stream the emulator to the TV using:

Sunshine on the server
Moonlight on a client device connected to the TV

For example:

Server (basement/office)
├─ Jellyfin
├─ DNS
├─ NAS storage
└─ PCSX2

TV
└─ Moonlight client

This works surprisingly well on gigabit Ethernet.

Option 3: Mini PC at the TV

Many people now use a small mini-PC:

5

The mini-PC:

Connects directly to the TV.
Runs PCSX2 locally.
Reads media and game files from the NAS.

This is often the best balance of cost, noise, and performance.

Storage Layout Example
NAS
│
├── Movies
├── TV Shows
├── Photos
├── Music
├── PS2
│   ├── Gran Turismo 4.iso
│   ├── God of War.iso
│   └── Shadow of the Colossus.iso
├── Backups
└── Documents

The gaming PC mounts the NAS share:

Z:\PS2\

and PCSX2 loads games from there.

Hardware I'd Recommend

For a modern home setup:

NAS

4–8 TB drives (or more)
16 GB RAM
Running:
TrueNAS or
Unraid

TV Device

Mini PC with:
Ryzen 5600U / 6600H or newer
16 GB RAM
SSD

This will emulate nearly the entire PS2 library at full speed.

One important note: make sure you own the original PS2 games and BIOS files where required, as emulator legality generally depends on using legally obtained game and BIOS copies.
and then I would be able to play e.g Ratchet and Clank on the pc using e.g a PS5 controller?

Yes.

If you have a PC running PCSX2 connected to your TV, you can play PS2 games such as the Ratchet & Clank series using a PS5 DualSense controller.

The setup would look like:

NAS
├─ PS2 game ISOs
├─ Movies
├─ Photos
└─ Other files

        │
        ▼
Living Room PC
├─ PCSX2
├─ Connected to TV via HDMI
└─ Paired with PS5 DualSense

        │
        ▼
TV

PCSX2 recognizes modern controllers and lets you map:

Left stick → PS2 left stick
Right stick → PS2 right stick
L1/L2/R1/R2 → PS2 shoulder buttons
Cross/Circle/Square/Triangle → PS2 face buttons

You can connect the DualSense:

Via USB-C cable
Via Bluetooth

The experience is generally excellent. Many PS2 games actually feel better with a DualSense than with an original PS2 controller because:

The sticks are more precise.
The controller is more comfortable.
Wireless support is built in.

For example, you could play:

Ratchet & Clank
Ratchet & Clank: Going Commando
Ratchet & Clank: Up Your Arsenal
Jak and Daxter: The Precursor Legacy
Sly Cooper and the Thievius Raccoonus

from your couch exactly like a console.

Many people also configure PCSX2 to launch directly into a full-screen "console mode" and set Windows to auto-start it, making the system behave almost like a dedicated retro console when the TV PC boots up. This works especially well if your NAS stores all the game ISOs and the TV PC simply accesses them over the network.
```