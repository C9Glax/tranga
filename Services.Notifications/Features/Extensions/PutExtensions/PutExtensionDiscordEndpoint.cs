using Extensions;
using Extensions.Data;
using Extensions.Extensions.NaprisExtensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Notifications.Database;
using Services.Notifications.Entities;
using Services.Notifications.Helpers;

namespace Services.Notifications.Features.Extensions.PutExtensions;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class PutExtensionDiscordEndpoint
{
    /// <summary>
    /// Add a Discord notification extension.
    /// </summary>
    /// <returns>The created extension</returns>
    /// <response code="200">The created extension</response>
    /// <response code="400">Extension could not be created</response>
    public static async Task<Results<Ok<NotificationExtension>, BadRequest>> Handle(NotificationsContext ctx, [FromBody]PutExtensionRequestDiscord req, CancellationToken ct)
    {
        DbNotificationExtension extension = new DbNapriseExtension(req.Name, req.ServiceUrl);

        await ctx.NotificationExtensions.AddAsync(extension, ct);
        await ctx.SaveChangesAsync(ct);

        await extension.CreateNotificationExtension().SendNotification(new Notification("Tranga is here!", Text: "Tranga can now send you notifications!"), ct);

        NotificationExtension result = new ()
        {
            Id = extension.Id,
            Name = extension.Name,
            Type = extension.Type
        };
        return TypedResults.Ok(result);
    }

    /// <summary>Request to add a Discord notification extension.</summary>
    /// <param name="Name">The user-assigned display name for the new extension instance.</param>
    /// <param name="WebhookId">The identifier portion of the Discord webhook URL used to send messages.</param>
    /// <param name="WebhookToken">The token portion of the Discord webhook URL used to authenticate the webhook.</param>
    public record PutExtensionRequestDiscord(string Name, string WebhookId, string WebhookToken)
        : PutExtensionNapriseEndpoint.PutExtensionRequestNaprise(Name, NotificationExtensionType.Discord, Discord.CreateServiceUrl(WebhookId, WebhookToken))
    {
        /// <inheritdoc cref="WebhookId" />
        public string WebhookId { get; init; } = WebhookId;
        /// <inheritdoc cref="WebhookToken" />
        public string WebhookToken { get; init; } = WebhookToken;
    }
}