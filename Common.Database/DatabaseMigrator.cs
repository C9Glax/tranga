using System.Diagnostics;
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
/// included, with a Postgres advisory lock. Advisory locks belong to a database, so the lock is taken on the default
/// <c>postgres</c> database, which exists before the application database does. Where <c>postgres</c> is missing or off
/// limits, the application database itself is used, as long as it exists already.
/// </para>
/// </remarks>
public static class DatabaseMigrator
{
    /// <summary>The advisory lock key every Tranga service takes while migrating (<c>TRANGA</c> as ASCII).</summary>
    private const long MigrationLockKey = 0x5452414E4741;

    /// <summary>
    /// The default maintenance database the lock is preferably taken on (EF Core's admin connection uses it as well):
    /// it exists before the application database does, so the lock also covers creating that one. It is not
    /// <c>template1</c>: <c>CREATE DATABASE</c> copies that one and fails with "source database "template1" is being
    /// accessed by other users" while any session, such as the one holding the lock, is connected to it.
    /// </summary>
    private const string AdminDatabase = "postgres";

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
        // Not every server has the postgres database, or lets this user connect to it (managed Postgres, for example).
        // Then the application database of the context is the next best thing; that only works once it exists.
        string[] candidates = template.Database is { Length: > 0 } applicationDatabase && applicationDatabase != AdminDatabase
            ? [AdminDatabase, applicationDatabase]
            : [AdminDatabase];
        DateTime deadline = DateTime.UtcNow + maxWait;
        TimeSpan delay = TimeSpan.FromMilliseconds(500);

        while (true)
        {
            try
            {
                return await OpenFirstAvailableAsync(template, candidates, ct);
            }
            catch (Exception e) when (IsStartingUp(e) && DateTime.UtcNow + delay < deadline)
            {
                logger?.LogWarning("Postgres is not ready yet ({Reason}), retrying in {Delay}s...", e.Message, delay.TotalSeconds);
                await Task.Delay(delay, ct);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 5));
            }
        }
    }

    /// <summary>Opens the first of the databases that exists and that the user may connect to.</summary>
    private static async Task<NpgsqlConnection> OpenFirstAvailableAsync(NpgsqlConnectionStringBuilder template, string[] databases, CancellationToken ct)
    {
        for (int i = 0; i < databases.Length; i++)
        {
            NpgsqlConnection connection = new(new NpgsqlConnectionStringBuilder(template.ConnectionString) { Database = databases[i] }.ConnectionString);
            try
            {
                await connection.OpenAsync(ct);
                return connection;
            }
            catch (PostgresException e) when (i < databases.Length - 1 && e.SqlState is PostgresErrorCodes.InvalidCatalogName or PostgresErrorCodes.InsufficientPrivilege)
            {
                await connection.DisposeAsync(); // Missing, or not allowed: try the next database.
            }
            catch (Exception)
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        throw new UnreachableException("The last database failing always throws.");
    }

    /// <summary>Whether the exception means Postgres is (still) unreachable or starting up, as opposed to a mistake in the configuration.</summary>
    private static bool IsStartingUp(Exception e) => e is SocketException or TimeoutException or NpgsqlException { IsTransient: true };
}
