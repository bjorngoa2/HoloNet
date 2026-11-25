using HoloNet.Video.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HoloNet.Video.Tests;

public sealed class VideoDatabaseMigratorShould : IDisposable
{
    private readonly InMemoryVideoDatabase _database = new();

    [Fact]
    public async Task UpgradeADatabaseCreatedBeforeMigrations_KeepingItsData()
    {
        // The schema EnsureCreated used to build, with a saved Prime login.
        _database.Execute("""
            CREATE TABLE "PrimeSessionCookies" (
                "Id" INTEGER NOT NULL CONSTRAINT "PK_PrimeSessionCookies" PRIMARY KEY AUTOINCREMENT,
                "Cookie" TEXT NOT NULL,
                "UpdatedUtc" TEXT NOT NULL);
            INSERT INTO "PrimeSessionCookies" ("Id", "Cookie", "UpdatedUtc") VALUES (1, 'session=abc', '2026-01-01 00:00:00+00:00');
            """);

        await MigrateAsync();

        await using var db = _database.CreateDbContext();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal("session=abc", (await db.PrimeSessionCookies.SingleAsync()).Cookie);
        Assert.Empty(await db.LibraryTitles.ToListAsync());
    }

    [Fact]
    public async Task CreateANewDatabase_AndBeSafeToRunAgain()
    {
        await MigrateAsync();
        await MigrateAsync();

        await using var db = _database.CreateDbContext();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Empty(await db.LibraryTitles.ToListAsync());
    }

    private Task MigrateAsync() =>
        new ServiceCollection()
            .AddSingleton<IDbContextFactory<HoloNetVideoDbContext>>(_database)
            .BuildServiceProvider()
            .MigrateVideoDatabaseAsync();

    public void Dispose() => _database.Dispose();
}
