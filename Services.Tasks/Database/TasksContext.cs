using Common.Database;
using Microsoft.EntityFrameworkCore;

namespace Services.Tasks.Database;

/// <summary>
/// EF Core database context for <c>Services.Tasks</c>, persisting the <see cref="DbWorker"/> observability
/// snapshots.
/// </summary>
public sealed class TasksContext(DbContextOptions<TasksContext> options) : TrangaDbContext<TasksContext>(options)
{
    /// <summary>Persisted snapshots of currently-running workers.</summary>
    public DbSet<DbWorker> Workers { get; init; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbWorker>()
            .HasKey(w => w.WorkerId);
    }
}
