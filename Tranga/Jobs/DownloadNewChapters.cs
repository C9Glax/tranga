using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class DownloadNewChapters : Job
{
    public Manga manga { get; set; }
    public string translatedLanguage { get; init; }

    public DownloadNewChapters(GlobalBase clone, MangaConnector connector, Manga manga, DateTime lastExecution,
        bool recurring = false, TimeSpan? recurrence = null, string? parentJobId = null, string translatedLanguage = "en") : base(clone, JobType.DownloadNewChaptersJob, connector, lastExecution, recurring,
        recurrence, parentJobId)
    {
        this.manga = manga;
        this.translatedLanguage = translatedLanguage;
    }
    
    public DownloadNewChapters(GlobalBase clone, MangaConnector connector, Manga manga, bool recurring = false, TimeSpan? recurrence = null, string? parentJobId = null, string translatedLanguage = "en") : base (clone, JobType.DownloadNewChaptersJob, connector, recurring, recurrence, parentJobId)
    {
        this.manga = manga;
        this.translatedLanguage = translatedLanguage;
    }

    protected override string GetId()
    {
        return $"{GetType()}-{manga.internalId}";
    }
    
    public override string ToString()
    {
        return $"{id} Manga: {manga}";
    }

    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal(JobBoss jobBoss)
    {
        manga.SaveSeriesInfoJson(settings.downloadLocation);
        Chapter[] chapters = mangaConnector.GetNewChapters(manga, this.translatedLanguage);
        this.progressToken.increments = chapters.Length;
        List<Job> jobs = new();
        mangaConnector.CopyCoverFromCacheToDownloadLocation(manga);
        foreach (Chapter chapter in chapters)
        {
            DownloadChapter downloadChapterJob = new(this, this.mangaConnector, chapter, parentJobId: this.id);
            jobs.Add(downloadChapterJob);
        }
        UpdateMetadata updateMetadataJob = new(this, this.mangaConnector, this.manga, parentJobId: this.id);
        jobs.Add(updateMetadataJob);
        progressToken.Complete();
        return jobs;
    }
}