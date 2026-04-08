using Services.Manga.Features.DownloadLinks;
using Services.Manga.Features.File;
using Services.Manga.Features.Manga;
using Services.Manga.Features.Manga.Search;
using Services.Manga.Features.Metadata;

namespace Services.Manga.Features;

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
        
        builder.MapGroup("/downloadLinks")
            .WithTags("Download")
            .MapDownloadEndpoints();
        
        builder.MapGroup("/files")
            .WithTags("Files")
            .MapFileEndpoints();
    }

    private static void MapMangaEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet(string.Empty, GetMangaListEndpoint.Handle);

        builder.MapGet("{mangaId}", GetMangaEndpoint.Handle);

        builder.MapGet("{mangaId}/cover", GetMangaCoverEndpoint.Handle);
        
        builder.MapGroup("/search").WithTags("Search").MapMangaSearchEndpoints();
        
        builder.MapGet("{mangaId}/metadata", GetMangaMetadataEndpoint.Handle)
            .WithTags("Metadata");
        
        builder.MapGet("{mangaId}/metadata/related", GetMangaMetadataEntriesEndpoint.Handle)
            .WithTags("Metadata");

        builder.MapPatch("{mangaId}/metadata/{metadataId}", PatchMangaMetadataEntryChosenEndpoint.Handle)
            .WithTags("Metadata");
        
        builder.MapGet("{mangaId}/downloadLinks", GetMangaDownloadLinksEndpoint.Handle)
            .WithTags("Download");
        
        builder.MapPatch("{mangaId}/downloadLinks/{downloadId}", PatchMangaDownloadLinkEndpoint.Handle)
            .WithTags("Download");
    }

    private static void MapMangaSearchEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost(string.Empty, PostSearchMangaEndpoint.Handle)
            .WithTags("Metadata");
        
        builder.MapPost("{mangaId}/downloadLinks", PostSearchMangaDownloadLinksEndpoint.Handle)
            .WithTags("Download");
    }

    private static void MapMetadataEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("/extensions", GetMetadataExtensionsEndpoint.Handle);
        
        builder.MapGet(string.Empty, GetMetadataEntriesEndpoint.Handle);

        builder.MapGet("{metadataId}", GetMetadataEntryEndpoint.Handle);

        builder.MapGet("{metadataId}/manga", GetMetadataMangaEndpoint.Handle)
            .WithTags("Manga");
        
        builder.MapGet("{metadataId}/manga/related", GetMetadataRelatedMangaIdsEndpoint.Handle)
            .WithTags("Manga");
    }

    private static void MapDownloadEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("/extensions", GetDownloadExtensionsEndpoint.Handle);
        
        builder.MapGet(string.Empty, GetDownloadLinksEndpoint.Handle);
        
        builder.MapGet("{downloadId}", GetDownloadLinkEndpoint.Handle);
    }

    private static void MapFileEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("{fileId}", GetFileEndpoint.Handle);
    }
}