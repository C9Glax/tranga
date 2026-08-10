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
public abstract class PutExtensionNtfyShEndpoint
{
    /// <summary>
    /// Add a Ntfy.sh notification extension.
    /// </summary>
    /// <returns>The created extension</returns>
    /// <response code="200">The created extension</response>
    /// <response code="400">Extension could not be created</response>
    public static async Task<Results<Ok<NotificationExtension>, BadRequest>> Handle(NotificationsContext ctx, [FromBody]PutExtensionRequestNtfySh req, CancellationToken ct)
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
        
    /// <summary>Request to add an Ntfy.sh notification extension.</summary>
    /// <param name="Name">The user-assigned display name for the new extension instance.</param>
    /// <param name="Host">The Ntfy.sh server host (optionally prefixed with a scheme, e.g. <c>https://</c>).</param>
    /// <param name="Port">The Ntfy.sh server port.</param>
    /// <param name="Topic">The topic to publish notifications to.</param>
    /// <param name="User">Optional username for authenticating with the Ntfy.sh server.</param>
    /// <param name="Password">Optional password for authenticating with the Ntfy.sh server.</param>
    public record PutExtensionRequestNtfySh(string Name, string Host, int Port, string Topic, string? User = null, string? Password = null)
        : PutExtensionNapriseEndpoint.PutExtensionRequestNaprise(Name, NotificationExtensionType.NtfySh, NtfySh.CreateServiceUrl(Host.StartsWith("https"), Host[(Host.IndexOf('/') + 2)..], Port, Topic, User, Password))
    {
        /// <inheritdoc cref="Host" />
        public string Host { get; init; } = Host;
        /// <inheritdoc cref="Port" />
        public int Port { get; init; } = Port;
        /// <inheritdoc cref="Topic" />
        public string Topic { get; init; } = Topic;
        /// <inheritdoc cref="User" />
        public string? User { get; init; } = User;
        /// <inheritdoc cref="Password" />
        public string? Password { get; init; } = Password;
    }
}