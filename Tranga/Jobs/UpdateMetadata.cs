using System.Runtime.CompilerServices;
using JobQueue;
using Microsoft.Extensions.Logging;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class UpdateMetadata : Job
{
    public Manga manga { get; set; }
    
    public UpdateMetadata(GlobalBase clone, JobQueue<MangaConnector> queue, MangaConnector connector, Manga manga, string? jobId = null, string? parentJobId = null, ILogger? logger = null) : base (clone, queue, connector, JobType.UpdateMetaDataJob, TimeSpan.Zero, TimeSpan.FromSeconds(clone.settings.jobTimeout), 1, jobId, parentJobId, logger)
    {
        this.manga = manga;
        if (jobId is null)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            this.JobId = $"{this.GetType().Name}-{connector.name}-{manga.sortName}-{new string(Enumerable.Repeat(chars, 4).Select(s => s[Random.Shared.Next(s.Length)]).ToArray())}";
        }
    }

    protected override void Execute(CancellationToken cancellationToken)
    {
        //Retrieve new Metadata
        Manga? possibleUpdatedManga = mangaConnector.GetMangaFromId(manga.publicationId);
        if (possibleUpdatedManga is { } updatedManga)
        {
            if (updatedManga.Equals(this.manga)) //Check if anything changed
            {
                this.ProgressToken.MarkFinished();
            }
            
            this.manga.UpdateMetadata(updatedManga);
            this.manga.SaveSeriesInfoJson(this.GlobalBase.settings.downloadLocation, true);
            this.ProgressToken.MarkFinished();
        }
        else
        {
            logger?.LogError($"Could not find Manga {manga}");
            this.ProgressToken.MarkFailed();
        }
        this.ProgressToken.Cancel();
        return ;
    }
}