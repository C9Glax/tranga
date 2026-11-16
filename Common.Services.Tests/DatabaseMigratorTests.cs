using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Common.Database;
using Common.Tests;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Common.Services.Tests;

public class DatabaseMigratorTests : TrangaTest
{
    /// <summary>A context without entities or migrations: migrating it only has to create the history table.</summary>
    private sealed class EmptyContext(DbContextOptions<EmptyContext> options) : DbContext(options);

    [Fact]
    public async Task MigrateWithLockAsync_WithoutNpgsql_MigratesDirectly()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync(ct);
        await using EmptyContext context = new(new DbContextOptionsBuilder<EmptyContext>().UseSqlite(connection).Options);

        await context.Database.MigrateWithLockAsync(ct);

        await using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE name = '__EFMigrationsHistory'";
        Assert.Equal(1L, await command.ExecuteScalarAsync(ct));
    }

    [Fact]
    public async Task MigrateWithLockAsync_PostgresUnreachable_RetriesUntilMaxWaitThenThrows()
    {
        await using EmptyContext context = new(new DbContextOptionsBuilder<EmptyContext>()
            .UseNpgsql($"Host=127.0.0.1;Port={GetClosedPort()};Database=tranga;Username=u;Password=p;Timeout=1")
            .Options);

        Stopwatch stopwatch = Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<NpgsqlException>(() => context.Database.MigrateWithLockAsync(ct, TimeSpan.FromSeconds(3)));
        stopwatch.Stop();

        // It does not give up on the first refused connection: the backoff waits 0.5s, then 1s, before the 3s are used up.
        Assert.True(stopwatch.Elapsed >= TimeSpan.FromSeconds(1), $"Gave up after {stopwatch.Elapsed}, expected it to retry first");
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(30), $"Took {stopwatch.Elapsed}, expected it to stop after maxWait");
    }

    [Fact]
    public async Task MigrateWithLockAsync_Cancelled_DoesNotRetry()
    {
        await using EmptyContext context = new(new DbContextOptionsBuilder<EmptyContext>()
            .UseNpgsql($"Host=127.0.0.1;Port={GetClosedPort()};Database=tranga;Username=u;Password=p;Timeout=1")
            .Options);
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();

        Stopwatch stopwatch = Stopwatch.StartNew();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => context.Database.MigrateWithLockAsync(cancelled.Token, TimeSpan.FromSeconds(30)));

        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Took {stopwatch.Elapsed}, a cancelled migration must not wait");
    }

    /// <summary>A local port nothing listens on, so connecting to it is refused straight away.</summary>
    private static int GetClosedPort()
    {
        TcpListener listener = new(IPAddress.Loopback, 0);
        listener.Start();
        int port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }
}
