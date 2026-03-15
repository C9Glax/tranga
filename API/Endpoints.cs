using API.Features;
using API.Features.Chapter;
using API.Features.Manga;

namespace API;

internal static class Endpoints
{
    internal static void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup("/manga").AddMangaEndpoints();
        builder.MapGroup("/chapter").AddChapterEndpoints();
    }

    private static void AddMangaEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("/search", PostSearchMangaEndpoint.Handle)
            .WithTags("Search");
        
        builder.MapPost("{mangaId}/watch", PostWatchMangaEndpoint.Handle)
            .WithTags("Download");
        
        builder.MapPost("{mangaId}/match", PostMatchMangaEndpoint.Handle)
            .WithTags("Search");
    }
    
    private static void AddChapterEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost("{chapterId}/download", PostDownloadChapter.Handle)
            .WithTags("Download");
    }
}