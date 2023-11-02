using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class UpdateMetadata : Job
{
    private Manga manga { get; set; }
    private JobBoss jobBoss { get; init; }

    public UpdateMetadata(GlobalBase clone, MangaConnector connector, Manga manga, JobBoss jobBoss, DateTime lastExecution, string? parentJobId = null) : base(clone, connector, lastExecution, parentJobId: parentJobId)
    {
        this.manga = manga;
        this.jobBoss = jobBoss;
    }
    
    public UpdateMetadata(GlobalBase clone, MangaConnector connector, Manga manga, JobBoss jobBoss, string? parentJobId = null) : base(clone, connector, parentJobId: parentJobId)
    public UpdateMetadata(GlobalBase clone, MangaConnector connector, Manga manga, string? parentJobId = null) : base(clone, JobType.UpdateMetaDataJob, connector, parentJobId: parentJobId)
    {
        this.manga = manga;
        this.jobBoss = jobBoss;
    }
    
    protected override string GetId()
    {
        return $"{GetType()}-{manga.internalId}";
    }

    public override string ToString()
    {
        return $"{id} Manga: {manga}";
    }

    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal()
    {
        if(manga.websiteUrl is null)
        {
            Log($"Legacy manga {manga}");
            return Array.Empty<Job>();
        }
        if (parentJobId is null)
        {
            Log($"Missing parentJob {this}");
            return Array.Empty<Job>();
        }
        Manga? possibleUpdatedManga = mangaConnector.GetMangaFromUrl(manga.websiteUrl);
        if (possibleUpdatedManga is { } updatedManga)
        {
            cachedPublications.Remove(this.manga);
            this.manga = updatedManga;
            cachedPublications.Add(updatedManga);
            this.manga.SaveSeriesInfoJson(settings.downloadLocation, true);

            DownloadNewChapters dncJob = this.jobBoss.GetJobById(this.parentJobId) as DownloadNewChapters ??
                                         throw new Exception("Jobtype has to be DownloadNewChapters");
            dncJob.manga = updatedManga;
        }
        else
        {
            Log($"Could not find Manga {manga}");
            return Array.Empty<Job>();
        }
        return Array.Empty<Job>();
    }
}