using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Notifications.Database;
using Services.Notifications.Entities;

namespace Services.Notifications.Features.Extensions;


/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetExtensionsEndpoint
{
    /// <summary>
    /// Returns all configured Notification Extensions
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="ct"></param>
    /// <returns>A list of all Notification Extensions that notifications will be sent to</returns>
    public static async Task<Ok<NotificationExtension[]>> Handle(NotificationsContext ctx, CancellationToken ct)
    {
        List<DbNotificationExtension> extensions = await ctx.NotificationExtensions.ToListAsync(ct);
        NotificationExtension[] result = extensions.Select(e => new NotificationExtension()
        {
            Id = e.Id,
            Name = e.Name,
            Type = e.Type
        }).ToArray();
        return TypedResults.Ok(result);
    }
}