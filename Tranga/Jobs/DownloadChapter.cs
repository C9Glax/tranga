using System.Net;
using JobQueue;
using Microsoft.Extensions.Logging;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class DownloadChapter : Job
{
    public Chapter chapter { get; init; }
    
    public DownloadChapter(GlobalBase clone, JobQueue<MangaConnector> queue, MangaConnector connector, Chapter chapter, int steps, string? jobId = null, string? parentJobId = null, ILogger? logger = null) : base (clone, queue, connector, JobType.DownloadChapterJob, TimeSpan.Zero, TimeSpan.FromSeconds(clone.settings.jobTimeout), steps, jobId, parentJobId, logger)
    {
        this.chapter = chapter;
        if (jobId is null)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            this.JobId = $"{this.GetType().Name}-{connector.name}-{chapter}-{new string(Enumerable.Repeat(chars, 4).Select(s => s[Random.Shared.Next(s.Length)]).ToArray())}";
        }
    }

    protected override void Execute(CancellationToken cancellationToken)
    {
        mangaConnector.CopyCoverFromCacheToDownloadLocation(chapter.parentManga);
        HttpStatusCode success = mangaConnector.DownloadChapter(chapter, this.ProgressToken);
        chapter.parentManga.UpdateLatestDownloadedChapter(chapter);
        if (success == HttpStatusCode.OK)
        {
            this.GlobalBase.UpdateLibraries();
            this.GlobalBase.SendNotifications("Chapter downloaded", $"{chapter.parentManga.sortName} - {chapter.chapterNumber}");
        }
    }

    public override bool Equals(object? obj)
    {
        if (obj is not DownloadChapter otherJob)
            return false;
        return otherJob.mangaConnector == this.mangaConnector &&
               otherJob.chapter.Equals(this.chapter);
    }
}