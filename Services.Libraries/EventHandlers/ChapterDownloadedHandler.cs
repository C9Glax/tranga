using Common.Services.Events;
using Common.Services.Events.Events;
using Extensions.Extensions;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Services.Libraries.Database;
using Services.Libraries.Helpers;

namespace Services.Libraries.EventHandlers;

internal sealed class ChapterDownloadedHandler(IChannel channel, IServiceProvider serviceProvider) : TrangaEventHandler<ChapterDownloadedEvent>(channel)
{
    protected override async Task<bool> HandleMessage(ChapterDownloadedEvent chapterDownloadedEvent)
    {
        LibrariesContext ctx = serviceProvider.GetRequiredService<LibrariesContext>();
        List<DbLibraryService> libraries = await ctx.LibraryServices.ToListAsync();
        foreach (DbLibraryService dbLibrary in libraries)
        {
            if (dbLibrary.LibraryServiceType == LibraryServiceType.Komga && dbLibrary.ToExtension() is { } extension)
            {
                return await ProcessKomga(ctx, dbLibrary, extension, chapterDownloadedEvent);
            }else continue;
        }

        return true;
    }

    private async Task<bool> ProcessKomga(LibrariesContext ctx, DbLibraryService dbLibrary, Extensions.Extensions.Komga komga, ChapterDownloadedEvent chapterDownloadedEvent)
    {
        if (await ctx.MangaMappings.SingleOrDefaultAsync(m => m.LibraryServiceId == dbLibrary.LibraryServiceId &&
                                                              m.MangaId == chapterDownloadedEvent.MangaId) is null)
        {
            KomgaSeries[] seriesList = await komga.GetSeriesList(CancellationToken.None);
            await komga.ScanLibrary(dbLibrary.TrangaLibraryId, CancellationToken.None);
            KomgaSeries[] newSeriesList;
            do
            {
                newSeriesList = await komga.GetSeriesList(CancellationToken.None);
                Thread.Sleep(1000);
            } while (newSeriesList.Length == seriesList.Length);

            KomgaSeries newSeries = newSeriesList.Single(l => seriesList.All(existing => existing.Id != l.Id));
            await ctx.MangaMappings.AddAsync(new DbMangaIdMapping(dbLibrary.LibraryServiceId, chapterDownloadedEvent.MangaId,
                newSeries.Id));
            await ctx.SaveChangesAsync();
        }
        else
        {
            await komga.ScanLibrary(dbLibrary.TrangaLibraryId, CancellationToken.None);
        }

        return true;
    }
}