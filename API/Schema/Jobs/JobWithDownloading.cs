using System.ComponentModel.DataAnnotations;
using API.Schema.MangaConnectors;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public abstract class JobWithDownloading : Job
{
    [StringLength(32)] [Required] public string MangaConnectorName { get; private set; } = null!;
    [JsonIgnore] private MangaConnector? _mangaConnector;
    [JsonIgnore] 
    public MangaConnector MangaConnector
    {
        get => LazyLoader.Load(this, ref _mangaConnector) ?? throw new InvalidOperationException();
        init
        {
            MangaConnectorName = value.Name;
            _mangaConnector = value;
        }
    }

    protected JobWithDownloading(string jobId, JobType jobType, ulong recurrenceMs, MangaConnector mangaConnector, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(jobId, jobType, recurrenceMs, parentJob, dependsOnJobs)
    {
        this.MangaConnector = mangaConnector;
    }

    /// <summary>
    /// EF CORE ONLY!!!
    /// </summary>
    internal JobWithDownloading(ILazyLoader lazyLoader, string jobId, JobType jobType, ulong recurrenceMs, string mangaConnectorName, string? parentJobId)
        : base(lazyLoader, jobId, jobType, recurrenceMs, parentJobId)
    {
        this.MangaConnectorName = mangaConnectorName;
    }
}