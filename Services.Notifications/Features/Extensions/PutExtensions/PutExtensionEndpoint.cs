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
    /// Generic base handler for the <c>PUT /extensions</c> route. Since <see cref="PutExtensionRequest"/> is abstract
    /// and carries no channel-specific configuration, this always rejects the request; callers must use one of the
    /// channel-specific endpoints (Naprise, Discord, Gotify, Ntfy.sh, Telegram) to actually create an extension.
    /// </summary>
    /// <returns>The created extension</returns>
    /// <response code="200">The created extension</response>
    /// <response code="400">Extension could not be created</response>
    public static async Task<Results<Ok<NotificationExtension>, BadRequest>> Handle(NotificationsContext ctx, [FromBody]PutExtensionRequest req, CancellationToken ct)
    {
        return TypedResults.BadRequest();
    }

    /// <summary>Base request shape shared by all notification-extension creation requests.</summary>
    /// <param name="Name">The user-assigned display name for the new extension instance.</param>
    /// <param name="NotificationExtensionType">The notification channel type being configured.</param>
    public abstract record PutExtensionRequest(string Name, NotificationExtensionType NotificationExtensionType)
    {
        /// <inheritdoc cref="Name" />
        public string Name { get; init; } = Name;
        /// <inheritdoc cref="NotificationExtensionType" />
        internal NotificationExtensionType NotificationExtensionType { get; init; } = NotificationExtensionType;
    }
}