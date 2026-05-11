using Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Notifications.Database;
using Services.Notifications.Entities;

namespace Services.Notifications.Features.Extensions.PutExtensions;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class PutExtensionEndpoint
{
    /// <summary>
    /// Add a notification extension.
    /// </summary>
    /// <returns>The created extension</returns>
    /// <response code="200">The created extension</response>
    /// <response code="400">Extension could not be created</response>
    public static async Task<Results<Ok<NotificationExtension>, BadRequest>> Handle(NotificationsContext ctx, [FromBody]PutExtensionRequest req, CancellationToken ct)
    {
        return TypedResults.BadRequest();
    }

    public abstract record PutExtensionRequest(string Name, NotificationExtensionType NotificationExtensionType)
    {
        public string Name { get; init; } = Name;
        internal NotificationExtensionType NotificationExtensionType { get; init; } = NotificationExtensionType;
    }
}