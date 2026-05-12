using Common.Services;
using Services.Notifications.Features.Extensions;
using Services.Notifications.Features.Extensions.PutExtensions;

namespace Services.Notifications.Features;

public sealed class Endpoints : EndpointsBuilder
{
    protected override void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup("/extensions")
            .WithTags("Notifications")
            .MapExtensionsEndpoints();
    }
}

internal static class EndpointHelpers
{
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