using Common.Services;
using Services.Tasks.Features.Status;
using Services.Tasks.Features.Tasks;

namespace Services.Tasks.Features;

internal class Endpoints : EndpointsBuilder
{
    protected override void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup(string.Empty)
            .WithTags("Tasks")
            .MapTaskEndpoints();

    }
}


internal static class EndpointHelpers
{
    internal static void MapTaskEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet(string.Empty, GetTaskListEndpoint.Handle)
            .WithSummary("Get all Tasks");
        
        builder.MapGet("{taskId}", GetTaskEndpoint.Handle)
            .WithSummary("Get Task");

        builder.MapGet("{taskId}/status", GetTaskStatusEndpoint.Handle)
            .WithSummary("Get status of a Task");
    }
}