using Common.Services.Events;
using Common.Services.Events.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Services.Libraries.Database;

namespace Services.Libraries.EventHandlers;

internal sealed class MangaMergedHandler(IChannel channel, IServiceProvider serviceProvider)
    : TrangaEventHandler<MangaMergedEvent>(channel)
{
    protected override async Task<bool> HandleMessage(MangaMergedEvent mangaMergedEvent)
    {
        ILogger<MangaMergedHandler> logger = serviceProvider.GetRequiredService<ILogger<MangaMergedHandler>>();
        using IServiceScope scope = serviceProvider.CreateScope();
        LibrariesContext librariesContext = scope.ServiceProvider.GetRequiredService<LibrariesContext>();

        List<DbMangaIdMapping> sourceMappings = await librariesContext.MangaMappings
            .Where(m => m.MangaId == mangaMergedEvent.SourceMangaId)
            .ToListAsync();

        if (sourceMappings.Count == 0)
            return true;

        foreach (DbMangaIdMapping sourceMapping in sourceMappings)
        {
            try
            {
                await ReconcileMapping(librariesContext, sourceMapping, mangaMergedEvent);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to reconcile library mapping {LibraryServiceId} from merged manga {SourceMangaId} onto {TargetMangaId}",
                    sourceMapping.LibraryServiceId, mangaMergedEvent.SourceMangaId, mangaMergedEvent.TargetMangaId);
            }
        }

        return true;
    }

    /// <summary>
    /// Re-points a mapping from the merged-away source manga onto the surviving target manga, or - if the target
    /// already has a mapping for the same library - drops the now-redundant source mapping instead.
    /// </summary>
    private static async Task ReconcileMapping(LibrariesContext librariesContext, DbMangaIdMapping sourceMapping, MangaMergedEvent mangaMergedEvent)
    {
        bool targetAlreadyMapped = await librariesContext.MangaMappings.AnyAsync(m =>
            m.MangaId == mangaMergedEvent.TargetMangaId && m.LibraryServiceId == sourceMapping.LibraryServiceId);

        librariesContext.MangaMappings.Remove(sourceMapping);

        if (!targetAlreadyMapped)
            await librariesContext.AddAsync(new DbMangaIdMapping(sourceMapping.LibraryServiceId, mangaMergedEvent.TargetMangaId, sourceMapping.SeriesId));

        await librariesContext.SaveChangesAsync();
    }
}
