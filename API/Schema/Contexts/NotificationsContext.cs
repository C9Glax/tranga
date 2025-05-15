using API.Schema.NotificationConnectors;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.Contexts;

public class NotificationsContext(DbContextOptions<NotificationsContext> options) : DbContext(options)
{
    public DbSet<NotificationConnector> NotificationConnectors { get; set; }
    public DbSet<Notification> Notifications { get; set; }
}