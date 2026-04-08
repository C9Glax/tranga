using Common.Services;

namespace Services.Tasks.Features;

internal class Endpoints : IEndpointsBuilder
{
    public void AddEndpoints(WebApplication app)
    {
        EndpointHelpers.AddEndpoints(app.MapGroup("/"));
    }
}

internal static class EndpointHelpers
{
    internal static void AddEndpoints(RouteGroupBuilder builder)
    {
        
    }
}