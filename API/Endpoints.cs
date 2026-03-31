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
        builder.MapGroup("/matches")
            .WithTags("Download")
            .AddMatchesEndpoints();
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
        
        builder.MapGet("{mangaId}/matched", GetMatchedEndpoint.Handle);
        
        builder.MapPatch("{mangaId}/monitor", PatchMangaMonitoredEndpoint.Handle)
            .WithTags("Download");
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
    }

    private static void AddMatchesEndpoints(this RouteGroupBuilder builder)
    {
        builder.MapGet("{matchId}", GetDownloadLinkEndpoint.Handle)
            .WithTags("Search");

        builder.MapPatch("{matchId}", PatchMatchedEndpoint.Handle)
            .WithTags("Search");
        
        builder.MapGet("{matchId}/chapters", GetChaptersEndpoint.Handle);
    }
}