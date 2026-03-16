using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Database;

public abstract class TrangaDatabaseContext<T>(DbContextOptions<T> options) : DbContext(options) where T : TrangaDatabaseContext<T>
{
    public async Task ApplyMigrations(CancellationToken? cancellationToken = null)
    {
        CancellationToken ct = cancellationToken ?? CancellationToken.None;
        try
        {
            IEnumerable<string> pendingMigrations = await Database.GetPendingMigrationsAsync(ct);
            if (pendingMigrations.Any())
            {
                Console.WriteLine("Applying pending migrations...");
                await Database.MigrateAsync(ct);
                Console.WriteLine("Migrations applied successfully.");
            }
            else
            {
                Console.WriteLine("No pending migrations found.");
            }
        }
        catch (NpgsqlException)
        {
            throw;
        }
    }
}