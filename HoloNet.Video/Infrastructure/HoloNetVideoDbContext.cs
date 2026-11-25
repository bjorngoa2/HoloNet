using HoloNet.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace HoloNet.Video.Infrastructure;

/// <summary>
/// The Video service's own SQLite database - deliberately not shared with any other HoloNet
/// service, mirroring the "services don't call each other" architecture: each service owns its
/// runtime state independently, so a schema change or lock contention here can never affect
/// Photos, Games, or TvLauncher. Holds runtime/mutable state that used to live in ad-hoc JSON
/// files (starting with the Prime Video session cookie - see <see cref="PrimeVideoSessionStore"/>),
/// as opposed to static app configuration, which stays in appsettings.json/env vars as before.
/// </summary>
public class HoloNetVideoDbContext(DbContextOptions<HoloNetVideoDbContext> options) : DbContext(options)
{
    public DbSet<PrimeSessionCookie> PrimeSessionCookies => Set<PrimeSessionCookie>();

    public DbSet<LibraryTitle> LibraryTitles => Set<LibraryTitle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrimeSessionCookie>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Cookie).IsRequired();
        });

        modelBuilder.Entity<LibraryTitle>(entity =>
        {
            entity.HasKey(e => new { e.Provider, e.ProviderMediaId });
            entity.Property(e => e.Provider).HasConversion<string>();
            entity.Property(e => e.EntityType).HasConversion<string>();
            entity.Property(e => e.Title).IsRequired();
        });
    }
}

/// <summary>
/// A title the user has added to a streaming service's folder in the TV launcher (see
/// <see cref="Services.ILibraryService"/>). <see cref="Title"/>, <see cref="EntityType"/> and
/// <see cref="PosterUrl"/> are a snapshot taken when it was added, so the library can be listed
/// without a round-trip to the streaming site per title.
/// </summary>
public class LibraryTitle
{
    public MediaProvider Provider { get; set; }
    public required string ProviderMediaId { get; set; }
    public required string Title { get; set; }
    public MediaEntityType EntityType { get; set; }
    public string? PosterUrl { get; set; }
    public DateTimeOffset AddedUtc { get; set; }
}

/// <summary>
/// Single-row table holding the current Prime Video session cookie. <see cref="Id"/> is always 1 -
/// there is only ever one active session, so this is modeled as a singleton row rather than a
/// history table (the previous JSON file worked the same way: one value, overwritten in place).
/// </summary>
public class PrimeSessionCookie
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;
    public required string Cookie { get; set; }
    public DateTimeOffset UpdatedUtc { get; set; }
}
