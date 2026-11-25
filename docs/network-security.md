# Network security

HoloNet is intended to be reachable only from devices on the home LAN (wired or
Wi-Fi) — never from the public internet. This is enforced at the Nginx reverse
proxy, in front of every service.

## Local DNS: dnsmasq

Nginx routes requests by hostname (`portal.goa.no`, `videos.goa.no`, etc. —
see below), so every device on the LAN needs those hostnames to resolve to
the server's LAN IP. The router (a stock TP-Link Archer BE3600) has no local
DNS rewrite feature of its own, so a `dnsmasq` container (see
`dnsmasq/dnsmasq.conf`) handles this instead:

- `dnsmasq` answers `*.goa.no` lookups with this server's LAN IP, and
  forwards every other domain to a public upstream resolver (Cloudflare),
  so normal internet browsing is unaffected.
- The router's DHCP Server settings must have their **Primary DNS** field
  set to this server's LAN IP, so every device that gets an IP via DHCP is
  automatically told to use `dnsmasq` for DNS — no per-device configuration.
- The server's LAN IP should be fixed via the router's **Address
  Reservation** (DHCP) so `dnsmasq/dnsmasq.conf`'s hardcoded IP doesn't go
  stale if the router reassigns addresses.

## Current setup: LAN-only IP allowlist

`nginx/nginx.conf` defines a `geo $lan_client` block that only allows requests
from the home subnet (`192.168.0.0/24`) and `localhost`. Every `server` block
(`portal.goa.no`, `videos.goa.no`, `photos.goa.no`, `games.goa.no`) checks this
and returns `403 Forbidden` for anything outside that range:

```nginx
geo $lan_client {
    default        0;
    192.168.0.0/24 1;
    127.0.0.1      1;
}

server {
    ...
    if ($lan_client = 0) { return 403; }
    ...
}
```

If the home server's LAN subnet ever changes (e.g. a different router/ISP,
VLAN setup, etc.), update the CIDR range in this block accordingly.

This is paired with Docker-level isolation in `docker-compose.yml`: only the
`nginx` service publishes a port (`80:80`) to the host. `holonet-portal`,
`holonet-video`, `holonet-photos`, and `holonet-games` have no `ports:` entry
at all — they're only reachable from `nginx` over the internal `holonet`
bridge network, never directly from the host or LAN.

## Why no authentication (for now)

HoloNet only serves personal media (video/photos), a game library catalog,
and a dashboard — there's no financial data, and every service is already
gated to the home network by the IP allowlist above. Anyone who can reach
these endpoints already has access to the home Wi-Fi/network, which is judged
an acceptable trust boundary for a home media server.

This may not be right for every household (e.g. if guests routinely get Wi-Fi
access, or the network includes untrusted IoT devices) — see the alternative
below if a stricter boundary is wanted.

## Alternative: HTTP Basic Auth at the Nginx layer

If IP allowlisting alone isn't enough (e.g. login credentials wanted even for
LAN clients), add HTTP Basic Auth in Nginx rather than in each ASP.NET Core
service — one place to manage, no per-service code changes, and it protects
every route (including static assets) uniformly.

1. Generate a password file (requires `htpasswd`, from the `apache2-utils` /
   `httpd-tools` package, or `openssl passwd`):
   ```sh
   htpasswd -c nginx/.htpasswd goa
   ```
   (Use `-c` only the first time — it overwrites the file. Add more users
   without `-c`.)

2. Mount it into the Nginx container in `docker-compose.yml`:
   ```yaml
   nginx:
     volumes:
       - ./nginx/nginx.conf:/etc/nginx/nginx.conf:ro
       - ./nginx/.htpasswd:/etc/nginx/.htpasswd:ro
   ```

3. Add to each `server` block in `nginx/nginx.conf` (alongside, not instead
   of, the existing `$lan_client` check):
   ```nginx
   auth_basic           "HoloNet";
   auth_basic_user_file /etc/nginx/.htpasswd;
   ```

4. **Do not commit `nginx/.htpasswd`** to git — add it to `.gitignore` if this
   is adopted, the same way `appsettings.local.json` is handled.

Basic Auth sends credentials base64-encoded (not encrypted) on every request,
so this is only appropriate over plain HTTP on a trusted LAN as an extra
speed bump — not a substitute for TLS if this were ever exposed beyond the
home network.
