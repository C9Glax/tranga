using Extensions;
using Extensions.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Notifications.Database;
using Services.Notifications.Entities;
using Services.Notifications.Helpers;

namespace Services.Notifications.Features.Extensions.PutExtensions;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class PutExtensionNapriseEndpoint
{
    /// <summary>
    /// Add a Naprise notification extension.
    /// </summary>
    /// <returns>The created extension</returns>
    /// <response code="200">The created extension</response>
    /// <response code="400">Extension could not be created</response>
    public static async Task<Results<Ok<NotificationExtension>, BadRequest>> Handle(NotificationsContext ctx, [FromBody]PutExtensionRequestNapriseServiceUrl req, CancellationToken ct)
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

    /// <summary>Request to add a Naprise notification extension by directly specifying its fully-formed service URL.</summary>
    /// <param name="Name">The user-assigned display name for the new extension instance.</param>
    /// <param name="NotificationExtensionType">The notification channel type being configured.</param>
    /// <param name="ServiceUrl">The Naprise service URL identifying the notification channel and its configuration.</param>
    public record PutExtensionRequestNapriseServiceUrl(string Name, NotificationExtensionType NotificationExtensionType, string ServiceUrl) : PutExtensionRequestNaprise(Name, NotificationExtensionType, ServiceUrl)
    {
        /// <inheritdoc cref="ServiceUrl" />
        public new string ServiceUrl { get; init; } = ServiceUrl;
    }

    /// <summary>Base request shape for Naprise-backed notification extensions, all of which resolve to a single service URL.</summary>
    /// <param name="Name">The user-assigned display name for the new extension instance.</param>
    /// <param name="NotificationExtensionType">The notification channel type being configured.</param>
    /// <param name="ServiceUrl">The Naprise service URL identifying the notification channel and its configuration.</param>
    public record PutExtensionRequestNaprise(string Name, NotificationExtensionType NotificationExtensionType, string ServiceUrl) : PutExtensionEndpoint.PutExtensionRequest(Name, NotificationExtensionType)
    {
        /// <inheritdoc cref="ServiceUrl" />
        internal string ServiceUrl { get; init; } = ServiceUrl;
    }
}