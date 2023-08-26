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
        this.jobs.Add(job);
    }

    public void RemoveJob(Job job)
    {
        job.Cancel();
        this.jobs.Remove(job);
    }

    public void RemoveJobs(IEnumerable<Job> jobs)
    {
        foreach (Job job in jobs)
        {
            job.Cancel();
            this.jobs.Remove(job);
        }
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
                return job.chapter.parentPublication.internalId == internalId &&
                       job.chapter.chapterNumber == chapterNumber;
            });
        else if (internalId is not null)
            ret = ret.Where(jjob =>
            {
                if (jjob is not DownloadNewChapters job)
                    return false;
                return job.publication.internalId == internalId;
            });
        return ret;
    }

    public IEnumerable<Job> GetJobsLike(MangaConnector? mangaConnector = null, Publication? publication = null,
        Chapter? chapter = null)
    {
        return GetJobsLike(mangaConnector?.name, publication?.internalId, chapter?.chapterNumber);
    }

    private bool QueueContainsJob(Job job)
    {
        mangaConnectorJobQueue.TryAdd(job.mangaConnector, new Queue<Job>());
        return mangaConnectorJobQueue[job.mangaConnector].Contains(job);
    }

    private void AddJobToQueue(Job job)
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