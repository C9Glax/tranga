using Common.Services.Events;
using Common.Services.Events.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using Services.Libraries.Database;
using Services.Libraries.Helpers;

namespace Services.Libraries.EventHandlers;

internal sealed class ChapterDeletedHandler(IChannel channel, IServiceProvider serviceProvider) : TrangaEventHandler<ChapterDeletedEvent>(channel)
{
    protected override async Task<bool> HandleMessage(ChapterDeletedEvent chapterDeletedEvent)
    {
        LibrariesContext ctx = serviceProvider.GetRequiredService<LibrariesContext>();

        List<DbLibraryService> libraries = await ctx.LibraryServices
            .Where(l => ctx.MangaMappings.Any(m => m.LibraryServiceId == l.LibraryServiceId && m.MangaId == chapterDeletedEvent.MangaId))
            .ToListAsync();

        foreach (DbLibraryService dbLibrary in libraries)
        {
            if (dbLibrary.LibraryServiceType == LibraryServiceType.Komga && dbLibrary.ToExtension() is { } extension)
                await extension.ScanLibrary(dbLibrary.TrangaLibraryId, CancellationToken.None);
        }

        return true;
    }
}
