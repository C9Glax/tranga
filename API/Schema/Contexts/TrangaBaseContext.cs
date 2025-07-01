using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.Schema.Contexts;

public abstract class TrangaBaseContext<T>(DbContextOptions<T> options) : DbContext(options) where T : DbContext
{
    private ILog Log => LogManager.GetLogger(GetType());
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.LogTo(s =>
        {
            Log.Debug(s);
        }, Array.Empty<string>(), LogLevel.Warning, DbContextLoggerOptions.Level | DbContextLoggerOptions.Category | DbContextLoggerOptions.UtcTime);
    }
}