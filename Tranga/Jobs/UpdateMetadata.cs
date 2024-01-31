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
        //Retrieve new Metadata
        Manga? possibleUpdatedManga = mangaConnector.GetMangaFromId(manga.publicationId);
        if (possibleUpdatedManga is { } updatedManga)
        {
            if (updatedManga.Equals(this.manga)) //Check if anything changed
            {
                this.progressToken.Complete();
                return Array.Empty<Job>();
            }
            
            this.manga.UpdateMetadata(updatedManga);
            this.manga.SaveSeriesInfoJson(settings.downloadLocation, true);
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

    public override bool Equals(object? obj)
    {
        
        if (obj is not UpdateMetadata otherJob)
            return false;
        return otherJob.mangaConnector == this.mangaConnector &&
               otherJob.manga.Equals(this.manga);
    }
}