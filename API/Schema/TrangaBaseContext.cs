using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.Schema;

public abstract class TrangaBaseContext<T> : DbContext where T : DbContext
{
    private ILog Log { get; init; }

    protected TrangaBaseContext(DbContextOptions<T> options) : base(options)
    {
        this.Log =  LogManager.GetLogger(GetType());
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.LogTo(s =>
        {
            Log.Debug(s);
        }, Array.Empty<string>(), LogLevel.Warning, DbContextLoggerOptions.Level | DbContextLoggerOptions.Category | DbContextLoggerOptions.UtcTime);
    }

    internal async Task<(bool success, string? exceptionMessage)> Sync(CancellationToken token, Type? trigger = null, string? reason = null)
    {
        int changesCount = ChangeTracker.Entries().Count(e => e.State is not EntityState.Unchanged and not EntityState.Detached);
        Log.DebugFormat("Syncing {0} changes {1} {2} {3}...", changesCount, GetType().Name, trigger?.Name, reason);
        if (changesCount < 1)
            return (true, null);
        try
        {
            int changedRows = await this.SaveChangesAsync(token);
            Log.DebugFormat("Synced {0} rows...", changedRows);
            return (true, null);
        }
        catch (Exception e)
        {
            Log.ErrorFormat("Syncing {0} changes {1} {2} {3} failed: {4}", changesCount, GetType().Name, trigger?.Name, reason, e);
            return (false, e.Message);
        }
    }

    public override string ToString() => $"{GetType().Name} {typeof(T).Name}";
}