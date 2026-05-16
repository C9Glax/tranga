using Common.Services;
using Services.Libraries.Features.Libraries;

namespace Services.Libraries.Features;

public sealed class Endpoints : EndpointsBuilder
{
    protected override void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup(string.Empty)
            .WithTags("Libraries").ConfigureLibrariesEndpoints();
    }
}

internal static class EndpointHelpers
{
    internal static void ConfigureLibrariesEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet(string.Empty, GetLibrariesEndpoint.Handle)
            .WithSummary("List of all configured library extensions");
    }
}