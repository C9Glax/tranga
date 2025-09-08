using API.Schema.NotificationsContext.NotificationConnectors;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.NotificationsContext;

public class NotificationsContext(DbContextOptions<NotificationsContext> options) : TrangaBaseContext<NotificationsContext>(options)
{
    public DbSet<NotificationConnector> NotificationConnectors { get; set; }
    public DbSet<Notification> Notifications { get; set; }
}