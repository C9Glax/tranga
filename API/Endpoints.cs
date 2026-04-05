using API.Features.File;
using API.Features.Manga;
using API.Features.Metadata;
using API.Features.Search;

namespace API;

internal static class Endpoints
{
    internal static void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup("/mangas")
            .WithTags("Manga")
            .MapMangaEndpoints();
        
        builder.MapGroup("/metadata")
            .WithTags("Metadata")
            .MapMetadataEndpoints();
        
        builder.MapGroup("/files")
            .WithTags("Files")
            .MapFileEndpoints();
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

        builder.MapPatch("{mangaId}/useMetadata", PatchMangaMetadataEntryEndpoint.Handle)
            .WithTags("Metadata");
        
        builder.MapGet("{mangaId}/metadata", GetMangaMetadataSourcesEndpoint.Handle)
            .WithTags("Info", "Metadata");
    }

    private static void MapMetadataEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet(string.Empty, GetMetadataListEndpoint.Handle);

        builder.MapGet("{metadataId}", GetMetadataEndpoint.Handle);
    }

    private static void MapFileEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("{fileId}", GetFileEndpoint.Handle);
    }
}