using Common.Database;
using Extensions;
using Microsoft.EntityFrameworkCore;

namespace Services.Notifications.Database;

/// <summary>
/// EF Core database context for <c>Services.Notifications</c>, persisting configured notification extensions.
/// </summary>
public sealed class NotificationsContext : TrangaDbContext<NotificationsContext>
{
    /// <summary>The configured notification extensions, stored using table-per-hierarchy discrimination on <see cref="DbNotificationExtension.Type"/>.</summary>
    public DbSet<DbNotificationExtension> NotificationExtensions { get; init; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DbNotificationExtension>()
            .HasDiscriminator<NotificationExtensionType>(e => e.Type)
            .HasValue<DbNapriseExtension>(NotificationExtensionType.Naprise);
    }
}