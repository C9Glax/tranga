using Common.Services;
using Services.Manga.Features.DownloadLinks;
using Services.Manga.Features.File;
using Services.Manga.Features.Manga;
using Services.Manga.Features.Manga.Search;
using Services.Manga.Features.Metadata;

namespace Services.Manga.Features;

internal class Endpoints : EndpointsBuilder
{
    protected override void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup(string.Empty)
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
}

internal static class EndpointHelpers
{
    internal static void MapMangaEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet(string.Empty, GetMangaListEndpoint.Handle)
            .WithSummary("List of all Manga");

        builder.MapGet("{mangaId}", GetMangaEndpoint.Handle)
            .WithSummary("Get Manga");

        builder.MapGet("{mangaId}/cover", GetMangaCoverEndpoint.Handle)
            .WithSummary("Cover of Manga");
        
        builder.MapGroup("/search").WithTags("Search").MapMangaSearchEndpoints();
        
        builder.MapGet("{mangaId}/metadata", GetMangaMetadataEndpoint.Handle)
            .WithSummary("Metadata of Manga")
            .WithTags("Metadata");
        
        builder.MapGet("{mangaId}/metadata/related", GetMangaMetadataEntriesEndpoint.Handle)
            .WithSummary("Metadata-Entries related to Manga")
            .WithTags("Metadata");

        builder.MapPatch("{mangaId}/metadata/{metadataId}", PatchMangaMetadataEntryChosenEndpoint.Handle)
            .WithSummary("Sets a Metadata-Entry as chosen \"Source of Truth\" for Manga")
            .WithTags("Metadata");
        
        builder.MapGet("{mangaId}/downloadLinks", GetMangaDownloadLinksEndpoint.Handle)
            .WithSummary("Download-Links of Manga")
            .WithTags("Download");
        
        builder.MapPatch("{mangaId}/downloadLinks/{downloadId}", PatchMangaDownloadLinkEndpoint.Handle)
            .WithSummary("Set Priority for Download-Link")
            .WithTags("Download");
    }

    private static void MapMangaSearchEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapPost(string.Empty, PostSearchMangaEndpoint.Handle)
            .WithSummary("Search Manga on Metadata-Extensions")
            .WithTags("Metadata");
        
        builder.MapPost("{mangaId}/downloadLinks", PostSearchMangaDownloadLinksEndpoint.Handle)
            .WithSummary("Search Manga on Download-Extensions")
            .WithTags("Download");
    }

    internal static void MapMetadataEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("/extensions", GetMetadataExtensionsEndpoint.Handle)
            .WithSummary("Get Metadata-Extensions");

        builder.MapGet(string.Empty, GetMetadataEntriesEndpoint.Handle)
            .WithSummary("List of all Metadata-Entries");

        builder.MapGet("{metadataId}", GetMetadataEntryEndpoint.Handle)
            .WithSummary("Get Metadata-Entry");

        builder.MapGet("{metadataId}/manga", GetMetadataMangaEndpoint.Handle)
            .WithSummary("Mangas the Metadata-Entry is linked to")
            .WithTags("Manga");
        
        builder.MapGet("{metadataId}/manga/related", GetMetadataRelatedMangaIdsEndpoint.Handle)
            .WithSummary("IDs of Manga the Metadata-Entry is related to")
            .WithTags("Manga");
    }

    internal static void MapDownloadEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("/extensions", GetDownloadExtensionsEndpoint.Handle)
            .WithSummary("Get Download-Extensions");

        builder.MapGet(string.Empty, GetDownloadLinksEndpoint.Handle)
            .WithSummary("List of all Download-Links");

        builder.MapGet("{downloadId}", GetDownloadLinkEndpoint.Handle)
            .WithSummary("Get Download-Link");
    }

    internal static void MapFileEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("{fileId}", GetFileEndpoint.Handle)
            .WithSummary("Get File");
    }
}