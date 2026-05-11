using Common.Database;
using Extensions;
using Microsoft.EntityFrameworkCore;

namespace Services.Notifications.Database;

public sealed class NotificationsContext : TrangaDbContext<NotificationsContext>
{
    public DbSet<DbNotificationExtension> NotificationExtensions { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbNotificationExtension>()
            .HasDiscriminator<NotificationExtensionType>(e => e.Type)
            .HasValue<DbNapriseExtension>(NotificationExtensionType.Naprise);
    }
}