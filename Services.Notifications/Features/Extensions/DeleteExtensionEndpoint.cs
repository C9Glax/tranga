using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Notifications.Database;

namespace Services.Notifications.Features.Extensions;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class DeleteExtensionEndpoint
{
    /// <summary>
    /// Deletes a Notification extension
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="extensionId">ID of the extension to remove</param>
    /// <param name="ct"></param>
    /// <response code="200">Extension removed</response>
    /// <response code="404">Extension with ID not found</response>
    public static async Task<Results<Ok, NotFound>> Handle(NotificationsContext ctx, [FromRoute]Guid extensionId, CancellationToken ct)
    {
        if (await ctx.NotificationExtensions.Where(ext => ext.Id == extensionId).ExecuteDeleteAsync(ct) < 1)
            return TypedResults.NotFound();
        return TypedResults.Ok();
    }
}