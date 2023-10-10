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
            UpdateJobFile(job);
        }
    }

    public void AddJobs(IEnumerable<Job> jobsToAdd)
    {
        foreach (Job job in jobsToAdd)
            AddJob(job);
    }

    /// <summary>
    /// Compares contents of the provided job and all current jobs
    /// Does not check if objects are the same
    /// </summary>
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
        UpdateJobFile(job);
    }

    public void RemoveJobs(IEnumerable<Job?> jobsToRemove)
    {
        List<Job?> toRemove = jobsToRemove.ToList(); //Prevent multiple enumeration
        Log($"Removing {toRemove.Count()} jobs.");
        foreach (Job? job in toRemove)
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
            return GetJobsLike(mangaConnector?.name, chapter.Value.parentManga.internalId, chapter.Value.chapterNumber);
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
        if (mangaConnectorJobQueue.TryAdd(job.mangaConnector, new Queue<Job>()))//If we can add the queue, there is certainly no job in it
            return true;
        return mangaConnectorJobQueue[job.mangaConnector].Contains(job);
    }

    public void AddJobToQueue(Job job)
    {
        Log($"Adding Job to Queue. {job}");
        if(!QueueContainsJob(job))
            mangaConnectorJobQueue[job.mangaConnector].Enqueue(job);
        job.ExecutionEnqueue();
    }

    private void AddJobsToQueue(IEnumerable<Job> newJobs)
    {
        foreach(Job job in newJobs)
            AddJobToQueue(job);
    }

    private void LoadJobsList(HashSet<MangaConnector> connectors)
    {
        if (!Directory.Exists(settings.jobsFolderPath)) //No jobs to load
        {
            Directory.CreateDirectory(settings.jobsFolderPath);
            return;
        }
        Regex idRex = new (@"(.*)\.json");

        //Load json-job-files
        foreach (FileInfo file in new DirectoryInfo(settings.jobsFolderPath).EnumerateFiles().Where(fileInfo => idRex.IsMatch(fileInfo.Name)))
        {
            Job job = JsonConvert.DeserializeObject<Job>(File.ReadAllText(file.FullName),
                new JobJsonConverter(this, new MangaConnectorJsonConverter(this, connectors)))!;
            this.jobs.Add(job);
        }
        
        //Connect jobs to parent-jobs and add Publications to cache
        foreach (Job job in this.jobs)
        {
            this.jobs.FirstOrDefault(jjob => jjob.id == job.parentJobId)?.AddSubJob(job);
            if(job is DownloadNewChapters dncJob)
                cachedPublications.Add(dncJob.manga);
        }
    }

    private void UpdateJobFile(Job job)
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

    private void UpdateAllJobFiles()
    {
        Log("Exporting Jobs");
        foreach (Job job in this.jobs)
            UpdateJobFile(job);

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
                if(!queueHead.recurring)
                    RemoveJob(queueHead);
                else
                    queueHead.ResetProgress();
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