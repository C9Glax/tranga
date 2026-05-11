using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using Services.Manga.Database;
using Services.Manga.Events;
using Services.Tasks.Tasks;
using Services.Tasks.WorkerLogic;
using Common.Services.Events;

namespace Services.Tasks.Events;

internal sealed class DownloadLinkModifiedHandler(IChannel channel, IServiceProvider serviceProvider) : TrangaEventHandler<DownloadLinkModifiedEvent>(channel)
{
    protected override async Task<bool> HandleMessage(DownloadLinkModifiedEvent notificationEvent)
    {
        MangaContext mangaContext = serviceProvider.CreateScope().ServiceProvider.GetRequiredService<MangaContext>();
        if (await mangaContext.MangaDownloadLinks.SingleOrDefaultAsync(link => link.DownloadLinkId == notificationEvent.DownloadLinkId) is not { } mangaDownloadLink)
            return false;
        
        GetMangaChaptersTask task = new (mangaDownloadLink.MangaId);
        return TasksCollection.RunOnceTasks.TryAdd(task.TaskId, task);
    }
}