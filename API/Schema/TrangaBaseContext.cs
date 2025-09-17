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

    internal async Task<(bool success, string? exceptionMessage)> Sync(CancellationToken token)
    {
        Log.Debug($"Syncing {GetType().Name}...");
        try
        {
            int changedRows = await this.SaveChangesAsync(token);
            Log.Debug($"Synced {changedRows} rows...");
            return (true, null);
        }
        catch (Exception e)
        {
            Log.Error("Sync failed:", e);
            return (false, e.Message);
        }
    }

    public override string ToString() => $"{GetType().Name} {typeof(T).Name}";
}