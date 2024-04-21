using System.Net;
using System.Text.RegularExpressions;
using Tranga.Jobs;

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
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobsCreateMonitorInternalId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobsCreateDownloadNewChaptersInternalId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobsCreateUpdateMetadata(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobsCreateUpdateMetadataInternalId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2JobJobId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if (groups.Count < 1 ||
            !_parent.jobBoss.TryGetJobById(groups[1].Value, out Job? job) ||
            job is null)
        {
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.BadRequest, $"Job with ID: '{groups[1].Value}' does not exist.");
        }
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, job);
    }
    
    private ValueTuple<HttpStatusCode, object?> DeleteV2JobJobId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
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
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
    private ValueTuple<HttpStatusCode, object?> PostV2JobJobIdCancel(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotImplemented, "Not Implemented");
    }
    
}