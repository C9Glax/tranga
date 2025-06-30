using Microsoft.EntityFrameworkCore.Infrastructure;

namespace API.Schema.Jobs;

public abstract class JobWithDownloading : Job
{

    public JobWithDownloading(string key, JobType jobType, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(key, jobType, recurrenceMs, parentJob, dependsOnJobs)
    {
        
    }
    public JobWithDownloading(ILazyLoader lazyLoader, string key, JobType jobType, ulong recurrenceMs, string? parentJobId)
        : base(lazyLoader, key, jobType, recurrenceMs, parentJobId)
    {
        
    }
}