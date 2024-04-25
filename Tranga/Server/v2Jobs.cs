using System.Net;
using System.Text.RegularExpressions;
using Tranga.Jobs;
using Tranga.MangaConnectors;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2Jobs(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, _parent.jobBoss.jobs.Select(job => job.id));
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2JobsRunning(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, _parent.jobBoss.jobs
            .Where(job => job.progressToken.state is ProgressToken.State.Running)
            .Select(job => job.id));
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2JobsWaiting(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, _parent.jobBoss.jobs
            .Where(job => job.progressToken.state is ProgressToken.State.Waiting)
            .Select(job => job.id));
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2JobsMonitoring(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, _parent.jobBoss.jobs
            .Where(job => job.jobType is Job.JobType.DownloadNewChaptersJob)
            .Select(job => job.id));
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2JobTypes(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK,
            Enum.GetValues<Job.JobType>().ToDictionary(b => (byte)b, b => Enum.GetName(b)));
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobCreateType(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !Enum.TryParse(groups[1].Value, true, out Job.JobType jobType))
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"JobType {groups[1].Value} does not exist.");
        }

        string? mangaId;
        Manga? manga;
        switch (jobType)
        {
            case Job.JobType.MonitorManga:
                if(!requestParameters.TryGetValue("internalId", out mangaId) ||
                   !_parent.TryGetPublicationById(mangaId, out manga) ||
                   manga is null)
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, "'internalId' Parameter missing, or is not a valid ID.");
                if(!requestParameters.TryGetValue("interval", out string? intervalStr) ||
                   !TimeSpan.TryParse(intervalStr, out TimeSpan interval))
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.InternalServerError, "'interval' Parameter missing, or is not in correct format.");
                requestParameters.TryGetValue("language", out string? language);
                if (requestParameters.TryGetValue("customFolder", out string? folder))
                    manga.Value.MovePublicationFolder(settings.downloadLocation, folder);
                if (requestParameters.TryGetValue("startChapter", out string? startChapterStr) &&
                    float.TryParse(startChapterStr, out float startChapter))
                {
                    Manga manga1 = manga.Value;
                    manga1.ignoreChaptersBelow = startChapter;
                }

                return _parent.jobBoss.AddJob(new DownloadNewChapters(this, ((Manga)manga).mangaConnector,
                        ((Manga)manga).internalId, true, interval, language)) switch
                    {
                        true => new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null),
                        false => new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.Conflict, "Job already exists."),
                    };
            case Job.JobType.UpdateMetaDataJob:
                if(!requestParameters.TryGetValue("internalId", out mangaId) ||
                   !_parent.TryGetPublicationById(mangaId, out manga) ||
                   manga is null)
                    return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, "InternalId Parameter missing, or is not a valid ID.");
                return _parent.jobBoss.AddJob(new UpdateMetadata(this, ((Manga)manga).internalId)) switch
                {
                    true => new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null),
                    false => new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.Conflict, "Job already exists."),
                };
            case Job.JobType.DownloadNewChaptersJob: //TODO
            case Job.JobType.DownloadChapterJob: //TODO
            default: return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.MethodNotAllowed, $"JobType {Enum.GetName(jobType)} is not supported.");
        }
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2JobJobId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !_parent.jobBoss.TryGetJobById(groups[1].Value, out Job? job) ||
            job is null)
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"Job with ID: '{groups[1].Value}' does not exist.");
        }
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, job);
    }
    
    private ValueTuple<HttpStatusCode, object?> DeleteV2JobJobId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !_parent.jobBoss.TryGetJobById(groups[1].Value, out Job? job) ||
            job is null)
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"Job with ID: '{groups[1].Value}' does not exist.");
        }

        _parent.jobBoss.RemoveJob(job);
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2JobJobIdProgress(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        
        if (groups.Count < 1 ||
            !_parent.jobBoss.TryGetJobById(groups[1].Value, out Job? job) ||
            job is null)
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.BadRequest, $"Job with ID: '{groups[1].Value}' does not exist.");
        }
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, job.progressToken);
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobJobIdStartNow(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !_parent.jobBoss.TryGetJobById(groups[1].Value, out Job? job) ||
            job is null)
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"Job with ID: '{groups[1].Value}' does not exist.");
        }
        _parent.jobBoss.AddJobs(job.ExecuteReturnSubTasks(_parent.jobBoss));
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobJobIdCancel(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !_parent.jobBoss.TryGetJobById(groups[1].Value, out Job? job) ||
            job is null)
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"Job with ID: '{groups[1].Value}' does not exist.");
        }
        job.Cancel();
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
    }
    
}