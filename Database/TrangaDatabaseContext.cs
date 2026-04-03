using Microsoft.EntityFrameworkCore;

namespace Database;

public abstract class TrangaDatabaseContext<T>(DbContextOptions<T> options) : DbContext(options) where T : TrangaDatabaseContext<T>
{
    public async Task ApplyMigrations(CancellationToken? cancellationToken = null)
    {
        CancellationToken ct = cancellationToken ?? CancellationToken.None;
        try
        {
            await Database.EnsureCreatedAsync(ct);
            IEnumerable<string> pendingMigrations = await Database.GetPendingMigrationsAsync(ct);
            foreach (string pendingMigration in pendingMigrations)
            {
                Console.WriteLine($"Applying Migration {pendingMigration}...");
                await Database.MigrateAsync(pendingMigration, ct);
                Console.WriteLine($"Migration {pendingMigration} applied!");
            }
        }
        catch (Exception)
        {
            // Probably build
        }
    }
}