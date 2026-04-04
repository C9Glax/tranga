using API.Features.Manga;
using API.Features.Search;

namespace API;

internal static class Endpoints
{
    internal static void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup("/mangas")
            .WithTags("Manga")
            .MapMangaEndpoints();
    }

    private static void MapMangaEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet(string.Empty, GetMangaListEndpoint.Handle)
            .WithTags("Info");

        builder.MapGet("{mangaId}", GetMangaEndpoint.Handle)
            .WithTags("Info");
        
        builder.MapGet("{mangaId}/cover", GetMangaCoverEndpoint.Handle)
            .WithTags("Info");
        
        builder.MapPost("/search", PostSearchMangaEndpoint.Handle)
            .WithTags("Search");
        
        builder.MapPost("{mangaId}/match", PostMatchMangaEndpoint.Handle)
            .WithTags("Search");
    }
}