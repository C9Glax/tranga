using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Tranga.MangaConnectors;

namespace Tranga.Jobs;

public class JobBoss : GlobalBase
{
    public HashSet<Job> jobs { get; init; }
    private Dictionary<MangaConnector, Queue<Job>> mangaConnectorJobQueue { get; init; }

    public JobBoss(GlobalBase clone, HashSet<MangaConnector> connectors) : base(clone)
    {
        this.jobs = new();
        LoadJobsList(connectors);
        this.mangaConnectorJobQueue = new();
        Log($"Next job in {jobs.MinBy(job => job.nextExecution)?.nextExecution.Subtract(DateTime.Now)} {jobs.MinBy(job => job.nextExecution)?.id}");
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
            ExportJob(job);
        }
    }

    public void AddJobs(IEnumerable<Job> jobsToAdd)
    {
        foreach (Job job in jobsToAdd)
            AddJob(job);
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
        if(job.subJobs is not null && job.subJobs.Any())
            RemoveJobs(job.subJobs);
        ExportJob(job);
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
        if (chapter is not null)
            return GetJobsLike(mangaConnector?.name, chapter.Value.parentManga.internalId, chapter?.chapterNumber);
        else
            return GetJobsLike(mangaConnector?.name, publication?.internalId);
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
        job.ExecutionEnqueue();
    }

    public void AddJobsToQueue(IEnumerable<Job> jobs)
    {
        foreach(Job job in jobs)
            AddJobToQueue(job);
    }

    public void LoadJobsList(HashSet<MangaConnector> connectors)
    {
        Directory.CreateDirectory(settings.jobsFolderPath);
        Regex idRex = new (@"(.*)\.json");

        foreach (FileInfo file in new DirectoryInfo(settings.jobsFolderPath).EnumerateFiles())
            if (idRex.IsMatch(file.Name))
            {
                Job job = JsonConvert.DeserializeObject<Job>(File.ReadAllText(file.FullName),
                    new JobJsonConverter(this, new MangaConnectorJsonConverter(this, connectors)))!;
                this.jobs.Add(job);
            }
                
        foreach (Job job in this.jobs)
            this.jobs.FirstOrDefault(jjob => jjob.id == job.parentJobId)?.AddSubJob(job);
        
        foreach (DownloadNewChapters ncJob in this.jobs.Where(job => job is DownloadNewChapters))
            cachedPublications.Add(ncJob.manga);
    }

    public void ExportJob(Job job)
    {
        string jobFilePath = Path.Join(settings.jobsFolderPath, $"{job.id}.json");
        
        if (!this.jobs.Any(jjob => jjob.id == job.id))
        {
            try
            {
                Log($"Deleting Job-file {jobFilePath}");
                while(IsFileInUse(jobFilePath))
                    Thread.Sleep(10);
                File.Delete(jobFilePath);
            }
            catch (Exception e)
            {
                Log(e.ToString());
            }
        }
        else
        {
            Log($"Exporting Job {jobFilePath}");
            string jobStr = JsonConvert.SerializeObject(job);
            while(IsFileInUse(jobFilePath))
                Thread.Sleep(10);
            File.WriteAllText(jobFilePath, jobStr);
        }
    }

    public void ExportJobsList()
    {
        Log("Exporting Jobs");
        foreach (Job job in this.jobs)
            ExportJob(job);

        //Remove files with jobs not in this.jobs-list
        Regex idRex = new (@"(.*)\.json");
        foreach (FileInfo file in new DirectoryInfo(settings.jobsFolderPath).EnumerateFiles())
        {
            if (idRex.IsMatch(file.Name))
            {
                string id = idRex.Match(file.Name).Groups[1].Value;
                if (!this.jobs.Any(job => job.id == id))
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception e)
                    {
                        Log(e.ToString());
                    }
                }
            }
        }
    }

    public void CheckJobs()
    {
        AddJobsToQueue(jobs.Where(job => job.progressToken.state == ProgressToken.State.Waiting && job.nextExecution < DateTime.Now && !QueueContainsJob(job)).OrderBy(job => job.nextExecution));
        foreach (Queue<Job> jobQueue in mangaConnectorJobQueue.Values)
        {
            if(jobQueue.Count < 1)
                continue;
            Job queueHead = jobQueue.Peek();
            if (queueHead.progressToken.state is ProgressToken.State.Complete or ProgressToken.State.Cancelled)
            {
                queueHead.ResetProgress();
                if(!queueHead.recurring)
                    RemoveJob(queueHead);
                jobQueue.Dequeue();
                Log($"Next job in {jobs.MinBy(job => job.nextExecution)?.nextExecution.Subtract(DateTime.Now)} {jobs.MinBy(job => job.nextExecution)?.id}");
            }else if (queueHead.progressToken.state is ProgressToken.State.Standby)
            {
                Job[] subJobs = jobQueue.Peek().ExecuteReturnSubTasks().ToArray();
                AddJobs(subJobs);
                AddJobsToQueue(subJobs);
            }else if (queueHead.progressToken.state is ProgressToken.State.Running && DateTime.Now.Subtract(queueHead.progressToken.lastUpdate) > TimeSpan.FromMinutes(5))
            {
                Log($"{queueHead} inactive for more than 5 minutes. Cancelling.");
                queueHead.Cancel();
            }
        }
    }
 }