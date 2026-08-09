using Common.Helpers;
using Common.Services.Events;
using Common.Services.Events.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Services.Libraries.Database;
using Services.Libraries.Helpers;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;

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
            return true;

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

        if (dbLibrary?.ToExtension() is not { } komga)
            return;

        if (await mangaContext.GetManga(mangaUpdatedEvent.MangaId, CancellationToken.None) is not { } mangaMetadataEntry)
            return;

        DbMetadata metadata = mangaMetadataEntry.Metadata;

        await komga.UpdateSeriesMetadata(new Extensions.Extensions.KomgaSeries(mapping.SeriesId, metadata.Series, metadata.Summary ?? ""),
            CancellationToken.None);

        if (metadata.CoverId is { } coverId)
        {
            if (await mangaContext.Files.FirstOrDefaultAsync(f => f.FileId == coverId) is { } file)
            {
                MemoryStream fileBytes = await file.LoadFile(CancellationToken.None);
                TrangaImage image = new();
                await fileBytes.CopyToAsync(image, CancellationToken.None);
                image.Position = 0;
                await komga.UpdateSeriesPoster(mapping.SeriesId, image, CancellationToken.None);
            }
        }
    }
}
