using Common.Services;
using Services.Tasks.Features.Tasks;
using Services.Tasks.Features.Tasks.Manga;

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
        
        builder.MapGroup("/create").CreateTaskEndpoints();
        
        builder.MapGroup("/manga").RelatedToMangaEndpoints();
    }
    
    private static void CreateTaskEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPut("getMangaChapters/{mangaId}", PutGetMangaChaptersTaskEndpoint.Handle)
            .WithSummary("Create a Task to get the Chapters of the Manga with requested ID.");
    }

    private static void RelatedToMangaEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("{mangaId}", GetMangaTasksEndpoint.Handle)
            .WithSummary("Get Tasks related to a Manga.");
    }
}