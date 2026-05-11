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
public abstract class PutExtensionGotifyEndpoint
{
    /// <summary>
    /// Add a Gotify notification extension.
    /// </summary>
    /// <returns>The created extension</returns>
    /// <response code="200">The created extension</response>
    /// <response code="400">Extension could not be created</response>
    public static async Task<Results<Ok<NotificationExtension>, BadRequest>> Handle(NotificationsContext ctx, [FromBody]PutExtensionRequestGotify req, CancellationToken ct)
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

    public record PutExtensionRequestGotify(string Name, string Host, int Port, string AppToken)
        : PutExtensionNapriseEndpoint.PutExtensionRequestNaprise(Name, NotificationExtensionType.Gotify, Gotify.CreateServiceUrl(Host.StartsWith("https"), Host[(Host.IndexOf('/') + 2)..], Port, AppToken))
    {
        public string Host { get; init; } = Host;
        public int Port { get; init; } = Port;
        public string AppToken { get; init; } = AppToken;
    }
}