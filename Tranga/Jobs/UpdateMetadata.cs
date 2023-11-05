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
        Manga? possibleUpdatedManga = mangaConnector.GetMangaFromId(manga.publicationId);
        if (possibleUpdatedManga is { } updatedManga)
        {
            if(updatedManga.Equals(this.manga))
                return Array.Empty<Job>();
            
            this.manga.UpdateMetadata(updatedManga);
            this.manga.SaveSeriesInfoJson(settings.downloadLocation, true);

            if (parentJobId is not null)
            {
                
                DownloadNewChapters dncJob = jobBoss.GetJobById(this.parentJobId) as DownloadNewChapters ??
                                             throw new Exception("Jobtype has to be DownloadNewChapters");
                dncJob.manga = updatedManga;
            }
            this.progressToken.Complete();
        }
        else
        {
            Log($"Could not find Manga {manga}");
            this.progressToken.Cancel();
            return Array.Empty<Job>();
        }
        this.progressToken.Cancel();
        return Array.Empty<Job>();
    }
}