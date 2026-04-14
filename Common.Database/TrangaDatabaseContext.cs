using Microsoft.EntityFrameworkCore;

namespace Common.Database;

public abstract class TrangaDatabaseContext<T>(DbContextOptions<T> options) : DbContext(options) where T : TrangaDatabaseContext<T>
{
    public async Task ApplyMigrations(CancellationToken? cancellationToken = null)
    {
        CancellationToken ct = cancellationToken ?? CancellationToken.None;
        try
        {
            await Database.MigrateAsync(ct);
        }
        catch (Exception)
        {
            // Probably build
        }
    }
}