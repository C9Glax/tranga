using Common.Services.Events;
using Common.Services.Events.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Services.Libraries.Database;
using Services.Libraries.Helpers;
using Services.Manga.Database;

namespace Services.Libraries.EventHandlers;

internal sealed class ChapterDownloadedHandler(IChannel channel, IServiceProvider serviceProvider) : TrangaEventHandler<ChapterDownloadedEvent>(channel)
{
    /// <summary>
    /// Bound on how many times we poll Komga for a newly discovered series after triggering a
    /// library scan, at ~1s per attempt (~30s total by default), so a misconfigured/unresponsive
    /// Komga instance can never block the RabbitMQ consumer thread indefinitely.
    /// Internal (not const/readonly) so tests can shrink the bound/interval instead of waiting ~30s.
    /// </summary>
    internal static int MaxSeriesPollAttempts = 30;

    internal static TimeSpan SeriesPollInterval = TimeSpan.FromSeconds(1);

    protected override async Task<bool> HandleMessage(ChapterDownloadedEvent chapterDownloadedEvent)
    {
        LibrariesContext ctx = serviceProvider.GetRequiredService<LibrariesContext>();
        MangaContext mangaContext = serviceProvider.GetRequiredService<MangaContext>();
        ILogger<ChapterDownloadedHandler> logger = serviceProvider.GetRequiredService<ILogger<ChapterDownloadedHandler>>();
        List<DbLibraryService> libraries = await ctx.LibraryServices.ToListAsync();

        bool allOk = true;
        foreach (DbLibraryService dbLibrary in libraries)
        {
            if (dbLibrary.LibraryServiceType == LibraryServiceType.Komga && dbLibrary.ToExtension() is { } extension)
            {
                bool ok = await ProcessKomga(ctx, mangaContext, dbLibrary, extension, chapterDownloadedEvent, logger);
                allOk &= ok;
            }
        }

        return allOk;
    }

    private static async Task<bool> ProcessKomga(LibrariesContext ctx, MangaContext mangaContext, DbLibraryService dbLibrary, Extensions.Extensions.Komga komga,
        ChapterDownloadedEvent chapterDownloadedEvent, ILogger<ChapterDownloadedHandler> logger)
    {
        if (await ctx.MangaMappings.AnyAsync(m => m.LibraryServiceId == dbLibrary.LibraryServiceId &&
                                                   m.MangaId == chapterDownloadedEvent.MangaId))
        {
            await komga.ScanLibrary(dbLibrary.TrangaLibraryId, CancellationToken.None);
            return true;
        }

        await komga.ScanLibrary(dbLibrary.TrangaLibraryId, CancellationToken.None);

        for (int attempt = 0; attempt < MaxSeriesPollAttempts; attempt++)
        {
            await KomgaSeriesLinker.LinkExistingMangaByName(ctx, mangaContext, dbLibrary, komga, logger, CancellationToken.None);
            await ctx.SaveChangesAsync();

            if (await ctx.MangaMappings.AnyAsync(m => m.LibraryServiceId == dbLibrary.LibraryServiceId &&
                                                       m.MangaId == chapterDownloadedEvent.MangaId))
                return true;

            await Task.Delay(SeriesPollInterval);
        }

        logger.LogWarning(
            "Timed out after {Attempts} attempts waiting for Komga library service {LibraryServiceId} to pick up the newly scanned series for manga {MangaId}. No mapping was created; the next ChapterDownloadedEvent for this manga will retry mapping discovery.",
            MaxSeriesPollAttempts, dbLibrary.LibraryServiceId, chapterDownloadedEvent.MangaId);
        return true;
    }
}