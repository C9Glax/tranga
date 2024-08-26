using Newtonsoft.Json;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class DownloadNewChapters : Job
{
    public string mangaInternalId { get; set; }
    [JsonIgnore] private Manga? manga => GetCachedManga(mangaInternalId);
    public string translatedLanguage { get; init; }

    public DownloadNewChapters(GlobalBase clone, string mangaInternalId, DateTime lastExecution, bool recurring = false, TimeSpan? recurrence = null, string? parentJobId = null, string translatedLanguage = "en") : base(clone, JobType.DownloadNewChaptersJob, lastExecution, recurring, recurrence, parentJobId)
    {
        this.mangaInternalId = mangaInternalId;
        this.translatedLanguage = translatedLanguage;
    }
    
    public DownloadNewChapters(GlobalBase clone, MangaConnector connector, string mangaInternalId, bool recurring = false, TimeSpan? recurrence = null, string? parentJobId = null, string translatedLanguage = "en") : base (clone, JobType.DownloadNewChaptersJob, recurring, recurrence, parentJobId)
    {
        this.mangaInternalId = mangaInternalId;
        this.translatedLanguage = translatedLanguage;
    }

    protected override string GetId()
    {
        return $"{GetType()}-{mangaInternalId}";
    }
    
    public override string ToString()
    {
        return $"{id} Manga: {manga}";
    }

    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal(JobBoss jobBoss)
    {
        if (manga is null)
        {
            Log($"Manga {mangaInternalId} is missing! Can not execute job.");
            return Array.Empty<Job>();
        }
        manga.Value.SaveSeriesInfoJson();
        Chapter[] chapters = manga.Value.mangaConnector.GetNewChapters(manga.Value, this.translatedLanguage);
        this.progressToken.increments = chapters.Length;
        List<Job> jobs = new();
        manga.Value.mangaConnector.CopyCoverFromCacheToDownloadLocation(manga.Value);
        foreach (Chapter chapter in chapters)
        {
            DownloadChapter downloadChapterJob = new(this, chapter, parentJobId: this.id);
            jobs.Add(downloadChapterJob);
        }
        UpdateMetadata updateMetadataJob = new(this, mangaInternalId, parentJobId: this.id);
        jobs.Add(updateMetadataJob);
        progressToken.Complete();
        return jobs;
    }

    protected override MangaConnector GetMangaConnector()
    {
        if (manga is null)
            throw new Exception($"Missing Manga {mangaInternalId}");
        return manga.Value.mangaConnector;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not DownloadNewChapters otherJob)
            return false;
        return otherJob.manga.Equals(this.manga);
    }
}