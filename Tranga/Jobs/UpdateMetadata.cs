using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class UpdateMetadata : Job
{
    public Manga manga { get; set; }
    
    public UpdateMetadata(GlobalBase clone, MangaConnector connector, Manga manga, string? parentJobId = null) : base(clone, JobType.UpdateMetaDataJob, connector, parentJobId: parentJobId)
    {
        this.manga = manga;
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
        if(manga.websiteUrl is null)
        {
            Log($"Legacy manga {manga}");
            return Array.Empty<Job>();
        }
        Manga? possibleUpdatedManga = mangaConnector.GetMangaFromUrl(manga.websiteUrl);
        if (possibleUpdatedManga is { } updatedManga)
        {
            cachedPublications.Remove(this.manga);
            this.manga = updatedManga;
            cachedPublications.Add(updatedManga);
            this.manga.SaveSeriesInfoJson(settings.downloadLocation, true);

            if (parentJobId is not null)
            {
                
                DownloadNewChapters dncJob = jobBoss.GetJobById(this.parentJobId) as DownloadNewChapters ??
                                             throw new Exception("Jobtype has to be DownloadNewChapters");
                dncJob.manga = updatedManga;
            }
        }
        else
        {
            Log($"Could not find Manga {manga}");
            return Array.Empty<Job>();
        }
        return Array.Empty<Job>();
    }
}