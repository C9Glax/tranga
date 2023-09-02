using Newtonsoft.Json;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class JobBoss : GlobalBase
{
    public HashSet<Job> jobs { get; init; }
    private Dictionary<MangaConnector, Queue<Job>> mangaConnectorJobQueue { get; init; }

    public JobBoss(GlobalBase clone, HashSet<MangaConnector> connectors) : base(clone)
    {
        if (File.Exists(settings.jobsFilePath))
            this.jobs = JsonConvert.DeserializeObject<HashSet<Job>>(File.ReadAllText(settings.jobsFilePath), new JobJsonConverter(this, new MangaConnectorJsonConverter(this, connectors)))!;
        else
            this.jobs = new();
        foreach (DownloadNewChapters ncJob in this.jobs.Where(job => job is DownloadNewChapters))
            cachedPublications.Add(ncJob.manga);
        this.mangaConnectorJobQueue = new();
    }

    public void AddJob(Job job)
    {
        if (ContainsJobLike(job))
        {
            Log($"Already Contains Job {job}");
        }
        else
        {
            Log($"Added {job}");
            this.jobs.Add(job);
            ExportJobsList();
        }
    }

    public bool ContainsJobLike(Job job)
    {
        if (job is DownloadChapter dcJob)
        {
            return this.GetJobsLike(dcJob.mangaConnector, chapter: dcJob.chapter).Any();
        }else if (job is DownloadNewChapters ncJob)
        {
            return this.GetJobsLike(ncJob.mangaConnector, ncJob.manga).Any();
        }

        return false;
    }

    public void RemoveJob(Job job)
    {
        Log($"Removing {job}");
        job.Cancel();
        this.jobs.Remove(job);
        if(job.subJobs is not null)
            RemoveJobs(job.subJobs);
        ExportJobsList();
    }

    public void RemoveJobs(IEnumerable<Job?> jobsToRemove)
    {
        Log($"Removing {jobsToRemove.Count()} jobs.");
        foreach (Job? job in jobsToRemove)
            if(job is not null)
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
        job.ExecuteNow();
    }

    public void AddJobsToQueue(IEnumerable<Job> jobs)
    {
        foreach(Job job in jobs)
            AddJobToQueue(job);
    }

    public void ExportJobsList()
    {
        Log($"Exporting {settings.jobsFilePath}");
        while(IsFileInUse(settings.jobsFilePath))
            Thread.Sleep(10);
        File.WriteAllText(settings.jobsFilePath, JsonConvert.SerializeObject(this.jobs));
    }

    public void CheckJobs()
    {
        foreach (Job job in jobs.Where(job => job.nextExecution < DateTime.Now && !QueueContainsJob(job)).OrderBy(job => job.nextExecution))
            AddJobToQueue(job);
        foreach (Queue<Job> jobQueue in mangaConnectorJobQueue.Values)
        {
            if(jobQueue.Count < 1)
                continue;
            Job queueHead = jobQueue.Peek();
            if (queueHead.progressToken.state is ProgressToken.State.Complete)
            {
                if(queueHead.recurring)
                    queueHead.ResetProgress();
                jobQueue.Dequeue();
            }else if (queueHead.progressToken.state is ProgressToken.State.Standby)
            {
                AddJobsToQueue(jobQueue.Peek().ExecuteReturnSubTasks());
            }
            else if (queueHead.progressToken.state is ProgressToken.State.Cancelled)
            {
                switch (queueHead)
                {
                    case DownloadChapter:
                        RemoveJob(queueHead);
                        break;
                    case DownloadNewChapters:
                        if(queueHead.recurring)
                            queueHead.progressToken.Complete();
                        break;
                }
                jobQueue.Dequeue();
            }
        }
    }
 }