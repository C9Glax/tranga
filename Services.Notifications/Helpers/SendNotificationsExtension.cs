using Extensions.Data;
using Microsoft.EntityFrameworkCore;
using Services.Notifications.Database;

namespace Services.Notifications.Helpers;

internal static class SendNotificationsExtension
{
    public static async Task<bool> SendNotifications(this DbSet<DbNotificationExtension> ctx, Notification notification, CancellationToken ct)
    {
        if (await ctx.ToListAsync(ct) is not { } extensions)
            return false;

        IEnumerable<Task> tasks = extensions.Select(ext => ext.CreateNotificationExtension().SendNotification(notification, ct));
        await Task.WhenAll(tasks);

        return true;
    }
}