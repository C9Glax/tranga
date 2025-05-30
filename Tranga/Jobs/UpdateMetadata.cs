﻿using Tranga.MangaConnectors;

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
            
            this.manga = manga.WithMetadata(updatedManga);
            this.manga.SaveSeriesInfoJson(true);
            this.mangaConnector.CopyCoverFromCacheToDownloadLocation(manga);
            foreach (Job job in jobBoss.GetJobsLike(publication: this.manga))
            {
                string oldFile;
                if (job is DownloadNewChapters dc)
                {
                    oldFile = dc.id;
                    dc.manga = this.manga;
                }
                else if (job is UpdateMetadata um)
                {
                    oldFile = um.id;
                    um.manga = this.manga;
                }
                else 
                    continue;
                jobBoss.UpdateJobFile(job, oldFile);
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

    public override bool Equals(object? obj)
    {
        
        if (obj is not UpdateMetadata otherJob)
            return false;
        return otherJob.mangaConnector == this.mangaConnector &&
               otherJob.manga.publicationId == this.manga.publicationId;
    }
}