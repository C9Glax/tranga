using System.Net.Sockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Common.Database;

/// <summary>
/// Applies EF Core migrations when several services start at the same time against one Postgres database.
/// </summary>
/// <remarks>
/// Every service migrates its own context on startup, and they all share one database and one
/// <c>__EFMigrationsHistory</c> table (Manga and Tasks even apply the very same <c>MangaContext</c> migrations).
/// Npgsql only locks that history table while the migrations are applied, so the steps EF takes before that lock -
/// creating the database and the history table - still race against each other: the loser of
/// <c>CREATE DATABASE</c> crashes with <c>42P04 database already exists</c>. On top of that Postgres can still be
/// starting up (first boot of the container) when the services come up.
/// <para>
/// <see cref="MigrateWithLockAsync"/> waits for Postgres and serializes the whole migration, database creation
/// included, with a Postgres advisory lock. Advisory locks belong to a database, so the lock is taken on the admin
/// database (<c>postgres</c>), which exists before the application database does.
/// </para>
/// </remarks>
public static class DatabaseMigrator
{
    /// <summary>The advisory lock key every Tranga service takes while migrating (<c>TRANGA</c> as ASCII).</summary>
    private const long MigrationLockKey = 0x5452414E4741;

    /// <summary>Admin databases that always exist, in the order the lock is tried on them (the same ones EF Core uses).</summary>
    private static readonly string[] AdminDatabases = ["postgres", "template1"];

    private static readonly TimeSpan DefaultMaxWait = TimeSpan.FromSeconds(90);

    /// <summary>
    /// Like <see cref="RelationalDatabaseFacadeExtensions.MigrateAsync"/>, but safe to call from several processes at
    /// once and while Postgres is still starting. Contexts that do not use Npgsql (in-memory or Sqlite in tests)
    /// are migrated directly.
    /// </summary>
    /// <param name="database">The database of the context to migrate</param>
    /// <param name="ct">Token to cancel waiting for Postgres, for the lock and the migration</param>
    /// <param name="maxWait">How long to keep retrying while Postgres is unreachable (default 90 seconds)</param>
    /// <param name="logger">Optional logger that reports waiting</param>
    /// <exception cref="NpgsqlException">Postgres was still unreachable after <paramref name="maxWait"/></exception>
    public static async Task MigrateWithLockAsync(this DatabaseFacade database, CancellationToken ct, TimeSpan? maxWait = null, ILogger? logger = null)
    {
        if (!database.IsNpgsql())
        {
            await database.MigrateAsync(ct);
            return;
        }

        string connectionString = database.GetConnectionString()
                                  ?? throw new InvalidOperationException("The context has no connection string to take the migration lock with.");

        // Not pooled: closing the connection ends the session, which releases the advisory lock.
        NpgsqlConnectionStringBuilder template = new(connectionString) { Pooling = false };

        await using NpgsqlConnection lockConnection = await OpenAdminConnectionAsync(template, maxWait ?? DefaultMaxWait, logger, ct);

        logger?.LogInformation("Waiting for the database migration lock...");
        await using (NpgsqlCommand lockCommand = new("SELECT pg_advisory_lock(@key)", lockConnection))
        {
            // Another service holding the lock is busy migrating, which can take a while.
            lockCommand.CommandTimeout = 300;
            lockCommand.Parameters.AddWithValue("key", MigrationLockKey);
            await lockCommand.ExecuteNonQueryAsync(ct);
        }

        logger?.LogInformation("Applying database migrations...");
        await database.MigrateAsync(ct);
    }

    /// <summary>Opens a connection to the admin database, retrying with backoff while Postgres is not accepting connections yet.</summary>
    private static async Task<NpgsqlConnection> OpenAdminConnectionAsync(NpgsqlConnectionStringBuilder template, TimeSpan maxWait, ILogger? logger, CancellationToken ct)
    {
        DateTime deadline = DateTime.UtcNow + maxWait;
        TimeSpan delay = TimeSpan.FromMilliseconds(500);

        while (true)
        {
            try
            {
                foreach (string adminDatabase in AdminDatabases)
                {
                    NpgsqlConnection connection = new(new NpgsqlConnectionStringBuilder(template.ConnectionString) { Database = adminDatabase }.ConnectionString);
                    try
                    {
                        await connection.OpenAsync(ct);
                        return connection;
                    }
                    catch (PostgresException e) when (e.SqlState == PostgresErrorCodes.InvalidCatalogName)
                    {
                        await connection.DisposeAsync(); // This admin database does not exist, try the next one.
                    }
                    catch (Exception)
                    {
                        await connection.DisposeAsync();
                        throw;
                    }
                }

                throw new InvalidOperationException($"None of the admin databases ({string.Join(", ", AdminDatabases)}) exists to take the migration lock on.");
            }
            catch (Exception e) when (IsStartingUp(e) && DateTime.UtcNow + delay < deadline)
            {
                logger?.LogWarning("Postgres is not ready yet ({Reason}), retrying in {Delay}s...", e.Message, delay.TotalSeconds);
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 5));
            }
        }
    }

    /// <summary>Whether the exception means Postgres is (still) unreachable or starting up, as opposed to a mistake in the configuration.</summary>
    private static bool IsStartingUp(Exception e) => e is SocketException or TimeoutException or NpgsqlException { IsTransient: true };
}
