using JobQueue;
using Microsoft.Extensions.Logging;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class DownloadNewChapter : Job
{
    public Manga manga { get; set; }
    public string translatedLanguage { get; init; }
    
    public DownloadNewChapter(GlobalBase clone, JobQueue<MangaConnector> queue, MangaConnector connector, Manga manga, TimeSpan interval, int steps, string? jobId = null, string? parentJobId = null, ILogger? logger = null, string translatedLanguage = "en") : base (clone, queue, connector, JobType.DownloadNewChaptersJob, interval, TimeSpan.FromSeconds(clone.settings.jobTimeout), steps, jobId, parentJobId, logger)
    {
        this.manga = manga;
        this.translatedLanguage = translatedLanguage;
        if (jobId is null)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            this.JobId = $"{this.GetType().Name}-{connector.name}-{manga.sortName}-{new string(Enumerable.Repeat(chars, 4).Select(s => s[Random.Shared.Next(s.Length)]).ToArray())}";
        }
    }

    protected override void Execute(CancellationToken cancellationToken)
    {
        manga.SaveSeriesInfoJson(GlobalBase.settings.downloadLocation);
        Chapter[] chapters = mangaConnector.GetNewChapters(manga, this.translatedLanguage);
        this.ProgressToken.SetSteps(chapters.Length);
        mangaConnector.CopyCoverFromCacheToDownloadLocation(manga);
        foreach (Chapter chapter in chapters)
        {
            DownloadChapter downloadChapterJob = new(this.GlobalBase, Queue, mangaConnector, chapter, 0, null,
                this.JobId, this.logger);
            Queue.AddJob(mangaConnector, downloadChapterJob);
        }
        UpdateMetadata updateMetadataJob = new(this.GlobalBase, Queue, mangaConnector, manga, null, this.JobId, this.logger);
        Queue.AddJob(mangaConnector, updateMetadataJob);
        this.ProgressToken.MarkFinished();
    }
}