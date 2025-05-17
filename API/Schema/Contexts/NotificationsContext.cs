using API.Schema.NotificationConnectors;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.Schema.Contexts;

public class NotificationsContext(DbContextOptions<NotificationsContext> options) : DbContext(options)
{
    public DbSet<NotificationConnector> NotificationConnectors { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    
    private ILog Log => LogManager.GetLogger(GetType());
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.LogTo(s =>
        {
            Log.Debug(s);
        }, [DbLoggerCategory.Query.Name], LogLevel.Trace, DbContextLoggerOptions.Level | DbContextLoggerOptions.Category);
    }
}