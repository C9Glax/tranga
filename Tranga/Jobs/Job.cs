using JobQueue;
using Microsoft.Extensions.Logging;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public abstract class Job : Job<MangaConnector>
{
    protected readonly GlobalBase GlobalBase;
    public MangaConnector mangaConnector { get; init; }
    public enum JobType : byte { DownloadChapterJob, DownloadNewChaptersJob, UpdateMetaDataJob }

    public JobType jobType;
    protected ILogger? logger;

    public Job(GlobalBase clone, JobQueue<MangaConnector> queue, MangaConnector connector, JobType jobType, TimeSpan interval, TimeSpan maximumTimeBetweenUpdates, int steps, string? jobId = null, string? parentJobId = null, ILogger? logger = null) : base(queue, interval, maximumTimeBetweenUpdates, steps, jobId, parentJobId, logger)
    {
        this.GlobalBase = clone;
        this.logger = logger;
        this.mangaConnector = connector;
        this.jobType = jobType;
    }
}