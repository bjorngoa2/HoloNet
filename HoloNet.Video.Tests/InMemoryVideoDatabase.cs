using HoloNet.Video.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace HoloNet.Video.Tests;

/// <summary>A private in-memory SQLite database, kept alive for the lifetime of the test.</summary>
internal sealed class InMemoryVideoDatabase : IDbContextFactory<HoloNetVideoDbContext>, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");

    public InMemoryVideoDatabase() => _connection.Open();

    public HoloNetVideoDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<HoloNetVideoDbContext>().UseSqlite(_connection).Options);

    public void Execute(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    public void Dispose() => _connection.Dispose();
}
