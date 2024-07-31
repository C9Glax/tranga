using System.Net;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class DownloadChapter : Job
{
    public Chapter chapter { get; init; }

    public DownloadChapter(GlobalBase clone, Chapter chapter, DateTime lastExecution, string? parentJobId = null) : base(clone, JobType.DownloadChapterJob, lastExecution, parentJobId: parentJobId)
    {
        this.chapter = chapter;
    }
    
    public DownloadChapter(GlobalBase clone, Chapter chapter, string? parentJobId = null) : base(clone, JobType.DownloadChapterJob, parentJobId: parentJobId)
    {
        this.chapter = chapter;
    }
    
    protected override string GetId()
    {
        return $"{GetType()}-{chapter.parentManga.internalId}-{chapter.chapterNumber}";
    }

    public override string ToString()
    {
        return $"{id} Chapter: {chapter}";
    }

    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal(JobBoss jobBoss)
    {
        Task downloadTask = new(delegate
        {
            mangaConnector.CopyCoverFromCacheToDownloadLocation(chapter.parentManga);
            HttpStatusCode success = mangaConnector.DownloadChapter(chapter, this.progressToken);
            chapter.parentManga.UpdateLatestDownloadedChapter(chapter);
            if (success == HttpStatusCode.OK)
            {
                UpdateLibraries();
                SendNotifications("Chapter downloaded", $"{chapter.parentManga.sortName} - {chapter.chapterNumber}");
            }
        });
        downloadTask.Start();
        return Array.Empty<Job>();
    }

    protected override MangaConnector GetMangaConnector()
    {
        return chapter.parentManga.mangaConnector;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not DownloadChapter otherJob)
            return false;
        return otherJob.chapter.Equals(this.chapter);
    }
}