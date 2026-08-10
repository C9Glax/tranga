using Common.Services;
using Services.Notifications.Features.Extensions;
using Services.Notifications.Features.Extensions.PutExtensions;

namespace Services.Notifications.Features;

/// <summary>
/// Root endpoint builder for the Notifications service; maps the <c>/extensions</c> route group used to manage notification extensions.
/// </summary>
public sealed class Endpoints : EndpointsBuilder
{
    /// <inheritdoc />
    protected override void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup("/extensions")
            .WithTags("Notifications")
            .MapExtensionsEndpoints();
    }
}

/// <summary>Extension methods that register the notification-extension endpoints on a route group.</summary>
internal static class EndpointHelpers
{
    /// <summary>
    /// Maps the CRUD endpoints for notification extensions: listing, generic/channel-specific creation (Naprise, Discord, Gotify, Ntfy.sh, Telegram), and deletion.
    /// </summary>
    /// <param name="builder">The route group to map the endpoints onto.</param>
    internal static void MapExtensionsEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet(string.Empty, GetExtensionsEndpoint.Handle)
            .WithSummary("Get configured notification extensions.");

        builder.MapPut(string.Empty, PutExtensionEndpoint.Handle)
            .WithSummary("Add a notification extension.");
        
        builder.MapPut("/naprise", PutExtensionNapriseEndpoint.Handle)
            .WithSummary("Add a Naprise notification extension.");
        
        builder.MapPut("/discord", PutExtensionDiscordEndpoint.Handle)
            .WithSummary("Add a Discord notification extension.");
        
        builder.MapPut("/gotify", PutExtensionGotifyEndpoint.Handle)
            .WithSummary("Add a Gotify notification extension.");
        
        builder.MapPut("/ntfysh", PutExtensionNtfyShEndpoint.Handle)
            .WithSummary("Add a NtfySh notification extension.");
        
        builder.MapPut("/telegram", PutExtensionTelegramEndpoint.Handle)
            .WithSummary("Add a Telegram notification extension.");
        
        builder.MapDelete("{extensionId}", DeleteExtensionEndpoint.Handle)
            .WithSummary("Remove a notification extension.");
    }
}