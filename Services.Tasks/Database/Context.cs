using Database;
using Microsoft.EntityFrameworkCore;

namespace Services.Tasks.Database;

internal class Context(DbContextOptions<Context> options) : TrangaDatabaseContext<Context>(options)
{
    internal DbSet<DbTask> Tasks { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbTask>()
            .HasKey(t => t.TaskTypeId);

        modelBuilder.Entity<DbTask>()
            .HasDiscriminator(t => t.TaskTypeId.GetTaskType())
            .HasValue<DbPeriodicTask>(TaskType.PeriodicTask)
            .HasValue<RunOnceTask>(TaskType.RunOnceTask);
    }
}