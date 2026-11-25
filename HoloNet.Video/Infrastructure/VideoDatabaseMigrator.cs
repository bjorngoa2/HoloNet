using System.Reflection;
using HoloNet.Video.Infrastructure.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HoloNet.Video.Infrastructure;

/// <summary>
/// Brings the service's SQLite database up to the latest EF Core migration at startup.
///
/// Databases created before migrations were introduced were built with <c>EnsureCreated</c>,
/// so they already contain the <see cref="InitialCreate"/> schema but have no migrations
/// history; applying <see cref="InitialCreate"/> to them would fail on the existing table.
/// Those databases are baselined first - <see cref="InitialCreate"/> is recorded as applied -
/// so only the migrations after it run.
/// </summary>
public static class VideoDatabaseMigrator
{
    public static async Task MigrateVideoDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var dbContextFactory = services.GetRequiredService<IDbContextFactory<HoloNetVideoDbContext>>();
        await using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

        if (await IsUnversionedLegacyDatabaseAsync(db, cancellationToken))
        {
            await BaselineAsync(db, cancellationToken);
        }

        await db.Database.MigrateAsync(cancellationToken);
    }

    private static async Task<bool> IsUnversionedLegacyDatabaseAsync(HoloNetVideoDbContext db, CancellationToken cancellationToken)
    {
        if (!await db.Database.CanConnectAsync(cancellationToken))
        {
            return false;
        }

        var historyRepository = db.GetService<IHistoryRepository>();
        if (await historyRepository.ExistsAsync(cancellationToken))
        {
            return false;
        }

        var legacyTableCount = await db.Database
            .SqlQuery<int>($"SELECT COUNT(*) AS \"Value\" FROM sqlite_master WHERE type = 'table' AND name = 'PrimeSessionCookies'")
            .SingleAsync(cancellationToken);
        return legacyTableCount > 0;
    }

    private static async Task BaselineAsync(HoloNetVideoDbContext db, CancellationToken cancellationToken)
    {
        var historyRepository = db.GetService<IHistoryRepository>();
        var initialMigrationId = typeof(InitialCreate).GetCustomAttribute<MigrationAttribute>()!.Id;

        await db.Database.ExecuteSqlRawAsync(historyRepository.GetCreateIfNotExistsScript(), cancellationToken);
        await db.Database.ExecuteSqlRawAsync(
            historyRepository.GetInsertScript(new HistoryRow(initialMigrationId, ProductInfo.GetVersion())), cancellationToken);
    }
}
