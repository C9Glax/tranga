using API.Schema.NotificationConnectors;
using log4net;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace API.Schema.Contexts;

public class NotificationsContext(DbContextOptions<NotificationsContext> options) : TrangaBaseContext<NotificationsContext>(options)
{
    public DbSet<NotificationConnector> NotificationConnectors { get; set; }
    public DbSet<Notification> Notifications { get; set; }
}