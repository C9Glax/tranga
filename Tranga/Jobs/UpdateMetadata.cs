using System.Text.Json.Serialization;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class UpdateMetadata : Job
{
    public string mangaInternalId { get; set; }
    [JsonIgnore] private Manga? manga => GetCachedManga(mangaInternalId);
    
    public UpdateMetadata(GlobalBase clone, string mangaInternalId, string? parentJobId = null) : base(clone, JobType.UpdateMetaDataJob, parentJobId: parentJobId)
    {
        this.mangaInternalId = mangaInternalId;
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

        //Retrieve new Metadata
        Manga? possibleUpdatedManga = mangaConnector.GetMangaFromId(manga.Value.publicationId);
        if (possibleUpdatedManga is { } updatedManga)
        {
            if (updatedManga.Equals(this.manga)) //Check if anything changed
            {
                this.progressToken.Complete();
                return Array.Empty<Job>();
            }
            
            AddMangaToCache(manga.Value.WithMetadata(updatedManga));
            this.manga.Value.SaveSeriesInfoJson(true);
            this.mangaConnector.CopyCoverFromCacheToDownloadLocation((Manga)manga);
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

    protected override MangaConnector GetMangaConnector()
    {
        if (manga is null)
            throw new Exception($"Missing Manga {mangaInternalId}");
        return manga.Value.mangaConnector;
    }

    public override bool Equals(object? obj)
    {
        
        if (obj is not UpdateMetadata otherJob)
            return false;
        return otherJob.manga.Equals(this.manga);
    }
}