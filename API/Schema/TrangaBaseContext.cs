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

    internal async Task<string?> Sync()
    {
        try
        {
            await this.SaveChangesAsync();
            return null;
        }
        catch (Exception e)
        {
            Log.Error(null, e);
            return e.Message;
        }
    }
}