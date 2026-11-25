using HoloNet.Video.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HoloNet.Video.Infrastructure;

/// <summary>
/// Mutable, runtime-updatable holder for the Prime Video session cookie. Exists because the only
/// way to obtain a fresh Prime Video session cookie is a semi-automated browser login flow (see
/// the standalone <c>HoloNet.PrimeAuth</c> tool, run on a machine with a display since this
/// service itself runs headless in a Docker container) — that flow pushes the freshly captured
/// cookie here via <c>PUT api/v1/admin/primevideo/session-cookie</c> at runtime, rather than
/// requiring a container restart with a new <c>PrimeVideo__SessionCookie</c> env var every time
/// the cookie expires.
///
/// Seeded from <see cref="PrimeVideoOptions.SessionCookie"/> at startup, then overridden by
/// whatever's persisted in the service's SQLite database (see <see cref="HoloNetVideoDbContext"/>)
/// if a row exists there (a previously-captured cookie is assumed fresher than a long-lived env
/// var). Every subsequent <see cref="Update"/> call persists back to that same row, so the
/// freshest cookie is always what's loaded on the next restart.
///
/// Registered as a singleton (the in-memory cookie needs to be instantly readable by every
/// request without a DB round-trip), so it can't hold a scoped <see cref="HoloNetVideoDbContext"/>
/// directly — it uses <see cref="IDbContextFactory{TContext}"/> to create short-lived contexts
/// for the initial load and each update instead.
/// </summary>
public class PrimeVideoSessionStore
{
    private readonly object _lock = new();
    private readonly IDbContextFactory<HoloNetVideoDbContext> _dbContextFactory;
    private string _cookie;
    private DateTimeOffset? _lastUpdatedUtc;

    public PrimeVideoSessionStore(IOptions<PrimeVideoOptions> options, IDbContextFactory<HoloNetVideoDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
        _cookie = options.Value.SessionCookie;

        using var db = _dbContextFactory.CreateDbContext();
        var persisted = db.PrimeSessionCookies.Find(PrimeSessionCookie.SingletonId);
        if (persisted is not null && !string.IsNullOrWhiteSpace(persisted.Cookie))
        {
            _cookie = persisted.Cookie;
            _lastUpdatedUtc = persisted.UpdatedUtc;
        }
    }

    public string Cookie
    {
        get
        {
            lock (_lock)
                return _cookie;
        }
    }

    public bool IsConfigured
    {
        get
        {
            lock (_lock)
                return !string.IsNullOrWhiteSpace(_cookie);
        }
    }

    public DateTimeOffset? LastUpdatedUtc
    {
        get
        {
            lock (_lock)
                return _lastUpdatedUtc;
        }
    }

    /// <summary>
    /// Replaces the current session cookie and persists it to the database, so a freshly captured
    /// login (from <c>HoloNet.PrimeAuth</c>) takes effect immediately for subsequent catalog
    /// lookups and survives a container restart.
    /// </summary>
    public void Update(string cookie)
    {
        lock (_lock)
        {
            _cookie = cookie;
            _lastUpdatedUtc = DateTimeOffset.UtcNow;

            using var db = _dbContextFactory.CreateDbContext();
            var row = db.PrimeSessionCookies.Find(PrimeSessionCookie.SingletonId);
            if (row is null)
            {
                db.PrimeSessionCookies.Add(new PrimeSessionCookie { Cookie = _cookie, UpdatedUtc = _lastUpdatedUtc.Value });
            }
            else
            {
                row.Cookie = _cookie;
                row.UpdatedUtc = _lastUpdatedUtc.Value;
            }

            db.SaveChanges();
        }
    }
}

