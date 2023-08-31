using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class JobBoss : GlobalBase
{
    public HashSet<Job> jobs { get; init; }
    private Dictionary<MangaConnector, Queue<Job>> mangaConnectorJobQueue { get; init; }

    public JobBoss(GlobalBase clone) : base(clone)
    {
        this.jobs = new();
        this.mangaConnectorJobQueue = new();
    }

    public void AddJob(Job job)
    {
        Log($"Added {job}");
        this.jobs.Add(job);
    }

    public void RemoveJob(Job job)
    {
        Log($"Removing {job}");
        job.Cancel();
        this.jobs.Remove(job);
    }

    public void RemoveJobs(IEnumerable<Job> jobsToRemove)
    {
        Log($"Removing {jobsToRemove.Count()} jobs.");
        foreach (Job job in jobsToRemove)
            RemoveJob(job);
    }

    public IEnumerable<Job> GetJobsLike(string? connectorName = null, string? internalId = null, string? chapterNumber = null)
    {
        IEnumerable<Job> ret = this.jobs;
        if (connectorName is not null)
            ret = ret.Where(job => job.mangaConnector.name == connectorName);
        
        if (internalId is not null && chapterNumber is not null)
            ret = ret.Where(jjob =>
            {
                if (jjob is not DownloadChapter job)
                    return false;
                return job.chapter.parentManga.internalId == internalId &&
                       job.chapter.chapterNumber == chapterNumber;
            });
        else if (internalId is not null)
            ret = ret.Where(jjob =>
            {
                if (jjob is not DownloadNewChapters job)
                    return false;
                return job.manga.internalId == internalId;
            });
        return ret;
    }

    public IEnumerable<Job> GetJobsLike(MangaConnector? mangaConnector = null, Manga? publication = null,
        Chapter? chapter = null)
    {
        return GetJobsLike(mangaConnector?.name, publication?.internalId, chapter?.chapterNumber);
    }

    public Job? GetJobById(string jobId)
    {
        if (this.jobs.FirstOrDefault(jjob => jjob.id == jobId) is { } job)
            return job;
        return null;
    }

    public bool TryGetJobById(string jobId, out Job? job)
    {
        if (this.jobs.FirstOrDefault(jjob => jjob.id == jobId) is { } ret)
        {
            job = ret;
            return true;
        }

        job = null;
        return false;
    }

    private bool QueueContainsJob(Job job)
    {
        mangaConnectorJobQueue.TryAdd(job.mangaConnector, new Queue<Job>());
        return mangaConnectorJobQueue[job.mangaConnector].Contains(job);
    }

    public void AddJobToQueue(Job job)
    {
        Log($"Adding Job to Queue. {job}");
        mangaConnectorJobQueue.TryAdd(job.mangaConnector, new Queue<Job>());
        Queue<Job> connectorJobQueue = mangaConnectorJobQueue[job.mangaConnector];
        if(!connectorJobQueue.Contains(job))
            connectorJobQueue.Enqueue(job);
    }

    public void AddJobsToQueue(IEnumerable<Job> jobs)
    {
        foreach(Job job in jobs)
            AddJobToQueue(job);
    }

    public void CheckJobs()
    {
        foreach (Job job in jobs.Where(job => job.nextExecution < DateTime.Now && !QueueContainsJob(job)).OrderBy(job => job.nextExecution))
            AddJobToQueue(job);
        foreach (Queue<Job> jobQueue in mangaConnectorJobQueue.Values)
        {
            Job queueHead = jobQueue.Peek();
            if (queueHead.progressToken.state == ProgressToken.State.Complete)
            {
                if(queueHead.recurring)
                    queueHead.Reset();
                jobQueue.Dequeue();
                AddJobsToQueue(jobQueue.Peek().ExecuteReturnSubTasks());
            }
        }
    }
 }