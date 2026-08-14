using Common.Services;
using Services.Manga.Features.Chapters;
using Services.Manga.Features.DownloadLinks;
using Services.Manga.Features.File;
using Services.Manga.Features.Manga;
using Services.Manga.Features.Manga.Search;
using Services.Manga.Features.Metadata;
using Services.Manga.Features.Suwayomi;

namespace Services.Manga.Features;

internal sealed class Endpoints : EndpointsBuilder
{
    protected override void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup(string.Empty)
            .WithTags("Manga")
            .MapMangaEndpoints();
        
        builder.MapGroup("/chapters")
            .WithTags("Manga", "Chapter")
            .MapChapterEndpoints();
        
        builder.MapGroup("/metadata")
            .WithTags("Manga", "Metadata")
            .MapMetadataEndpoints();
        
        builder.MapGroup("/downloadLinks")
            .WithTags("Manga", "Download")
            .MapDownloadEndpoints();
        
        builder.MapGroup("/files")
            .WithTags("Manga", "Files")
            .MapFileEndpoints();

        builder.MapGroup("/suwayomi")
            .WithTags("Manga", "Suwayomi")
            .MapSuwayomiEndpoints();
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

        builder.MapPatch("{mangaId}/monitored", PatchMangaMonitoredEndpoint.Handle)
            .WithSummary("Set the monitored status of a Manga");

        builder.MapPost("{mangaId}/sync", PostMangaSyncEndpoint.Handle)
            .WithSummary("Manually trigger a metadata sync to any linked library extensions (e.g. Komga)");

        builder.MapPost("{mangaId}/remove", PostMangaRemoveEndpoint.Handle)
            .WithSummary("Stop monitoring and un-choose a Manga, removing it from the Manga list");

        builder.MapPost("{mangaId}/merge", PostMangaMergeEndpoint.Handle)
            .WithSummary("Merge another Manga into this one, keeping this Manga's ID");

        builder.MapGet("{mangaId}/chapters", GetMangaChaptersEndpoint.Handle)
            .WithSummary("Chapters of Manga")
            .WithTags("Chapter");

        builder.MapGet("{mangaId}/downloadLinks", GetMangaDownloadLinksEndpoint.Handle)
            .WithSummary("Download-Links of Manga")
            .WithTags("Download");

        builder.MapPost("{mangaId}/downloadLinks", PostMangaDownloadLinkEndpoint.Handle)
            .WithSummary("Manually add a Download-Link for Manga by pasting its page URL on an extension's site")
            .WithTags("Download");

        builder.MapPatch("{mangaId}/downloadLinks/{downloadId}", PatchMangaDownloadLinkEndpoint.Handle)
            .WithSummary("Set Priority for Download-Link")
            .WithTags("Download");
    }

    internal static void MapChapterEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet(string.Empty, GetChaptersEndpoint.Handle)
            .WithSummary("Get Chapters");

        builder.MapGet("{chapterId}", GetChapterEndpoint.Handle)
            .WithSummary("Get Chapter");
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

    internal static void MapSuwayomiEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("/status", GetSuwayomiStatusEndpoint.Handle)
            .WithSummary("Status of the Suwayomi sidecar");

        builder.MapGet("/extensions", GetSuwayomiExtensionsEndpoint.Handle)
            .WithSummary("Get the Suwayomi extension catalogue");

        builder.MapPost("/extensions/{pkgName}/install", PostSuwayomiExtensionEndpoint.Install)
            .WithSummary("Install a Suwayomi extension");

        builder.MapPost("/extensions/{pkgName}/update", PostSuwayomiExtensionEndpoint.Update)
            .WithSummary("Update a Suwayomi extension");

        builder.MapDelete("/extensions/{pkgName}", PostSuwayomiExtensionEndpoint.Uninstall)
            .WithSummary("Uninstall a Suwayomi extension");

        builder.MapGet("/sources", GetSuwayomiSourcesEndpoint.Handle)
            .WithSummary("Get the sources of all installed Suwayomi extensions");

        builder.MapPost("/refresh", PostSuwayomiRefreshEndpoint.Handle)
            .WithSummary("Re-read the sources installed on the Suwayomi sidecar");
    }
}