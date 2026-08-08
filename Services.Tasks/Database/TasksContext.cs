using Common.Database;
using Microsoft.EntityFrameworkCore;

namespace Services.Tasks.Database;

public sealed class TasksContext(DbContextOptions<TasksContext> options) : TrangaDbContext<TasksContext>(options)
{
    public DbSet<DbWorker> Workers { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbWorker>()
            .HasKey(w => w.WorkerId);
    }
}
