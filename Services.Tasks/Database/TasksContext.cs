using Common.Database;
using Microsoft.EntityFrameworkCore;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Database;

public class TasksContext(DbContextOptions<TasksContext> options) : TrangaDatabaseContext<TasksContext>(options)
{
    internal DbSet<DbTask> Tasks { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbTask>()
            .HasKey(t => t.TaskId);

        modelBuilder.Entity<DbTask>()
            .HasDiscriminator(t => t.TaskType)
            .HasValue<DbPeriodicTask>(TaskType.PeriodicTask)
            .HasValue<DbRunOnceTask>(TaskType.RunOnceTask);
    }
}