using API.Features.Chapter;
using API.Features.DownloadExtensions;
using API.Features.File;
using API.Features.Manga;
using API.Features.MetadataExtensions;

namespace API;

internal static class Endpoints
{
    internal static void AddEndpoints(RouteGroupBuilder builder)
    {
        builder.MapGroup("/manga")
            .WithTags("Manga")
            .AddMangaEndpoints();
        builder.MapGroup("/chapter")
            .WithTags("Chapter")
            .AddChapterEndpoints();
        builder.MapGroup("/metadataExtensions").AddMetadataExtensionEndpoints();
        builder.MapGroup("/downloadExtensions").AddDownloadExtensionEndpoints();
        builder.MapGet("file/{fileId}", GetFileEndpoint.Handle).WithTags("File");
    }

    private static void AddMangaEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet(string.Empty, GetListMangaEndpoint.Handle);
        
        builder.MapGet("{mangaId}", GetMangaEndpoint.Handle);
        
        builder.MapGet("{mangaId}/cover", GetCoverEndpoint.Handle);
        
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

    private static void AddMetadataExtensionEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet(string.Empty, GetMetadataExtensionsEndpoint.Handle)
            .WithTags("Search");
    }

    private static void AddDownloadExtensionEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet(string.Empty, GetDownloadExtensionsEndpoint.Handle)
            .WithTags("Download");

        builder.MapGet("{downloadLinkId}", GetDownloadLinkEndpoint.Handle)
            .WithTags("Download", "Search");

        builder.MapPatch("{downloadLinkId}", PatchMatchedEndpoint.Handle)
            .WithTags("Download", "Search");
    }
}