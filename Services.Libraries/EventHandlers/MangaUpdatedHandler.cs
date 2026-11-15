using Common.Services.Events;
using Common.Services.Events.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Services.Libraries.Database;
using Services.Libraries.Helpers;
using Services.Manga.Database;

namespace Services.Libraries.EventHandlers;

internal sealed class MangaUpdatedHandler(IChannel channel, IServiceProvider serviceProvider)
    : TrangaEventHandler<MangaUpdatedEvent>(channel)
{
    protected override async Task<bool> HandleMessage(MangaUpdatedEvent mangaUpdatedEvent)
    {
        ILogger<MangaUpdatedHandler> logger = serviceProvider.GetRequiredService<ILogger<MangaUpdatedHandler>>();
        using IServiceScope scope = serviceProvider.CreateScope();
        LibrariesContext librariesContext = scope.ServiceProvider.GetRequiredService<LibrariesContext>();
        MangaContext mangaContext = scope.ServiceProvider.GetRequiredService<MangaContext>();

        List<DbMangaIdMapping> mappings = await librariesContext.MangaMappings
            .Where(m => m.MangaId == mangaUpdatedEvent.MangaId)
            .ToListAsync();

        if (mappings.Count == 0)
        {
            // SeriesLinker already pushes metadata for anything it newly links, so there's
            // nothing left to do here for this manga either way.
            await LinkToLibraries(librariesContext, mangaContext, logger);
            return true;
        }

        foreach (DbMangaIdMapping mapping in mappings)
        {
            try
            {
                await ProcessMapping(librariesContext, mangaContext, mapping, mangaUpdatedEvent);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to push metadata update to library {LibraryServiceId} for manga {MangaId}",
                    mapping.LibraryServiceId, mangaUpdatedEvent.MangaId);
            }
        }

        return true;
    }

    private async Task ProcessMapping(LibrariesContext librariesContext, MangaContext mangaContext, DbMangaIdMapping mapping,
        MangaUpdatedEvent mangaUpdatedEvent)
    {
        DbLibraryService? dbLibrary = await librariesContext.LibraryServices
            .SingleOrDefaultAsync(l => l.LibraryServiceId == mapping.LibraryServiceId);

        if (dbLibrary?.ToClient() is not { } client)
            return;

        await MetadataSync.PushMetadata(mangaContext, client, mapping.SeriesId, mangaUpdatedEvent.MangaId, CancellationToken.None);
    }

    private static async Task LinkToLibraries(LibrariesContext librariesContext, MangaContext mangaContext, ILogger logger)
    {
        List<DbLibraryService> libraries = await librariesContext.LibraryServices.ToListAsync();
        foreach (DbLibraryService dbLibrary in libraries)
        {
            if (dbLibrary.ToClient() is { } client)
                await SeriesLinker.LinkExistingMangaByName(librariesContext, mangaContext, dbLibrary, client, logger, CancellationToken.None);
        }

        await librariesContext.SaveChangesAsync();
    }
}
