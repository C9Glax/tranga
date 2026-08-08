using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Services.Tasks.Database;

/// <summary>
/// Lets <c>dotnet ef migrations</c> construct a <see cref="TasksContext"/> directly instead of building the full
/// <see cref="Service"/> host (which requires a reachable RabbitMQ instance) - only used by EF design-time
/// tooling, never at runtime, where <see cref="TasksContext"/> is resolved through normal DI instead.
/// </summary>
public sealed class TasksContextDesignTimeFactory : IDesignTimeDbContextFactory<TasksContext>
{
    public TasksContext CreateDbContext(string[] args)
    {
        DbContextOptions<TasksContext> options = new DbContextOptionsBuilder<TasksContext>().Options;
        return new TasksContext(options);
    }
}
