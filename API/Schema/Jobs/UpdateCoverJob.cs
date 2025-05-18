using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class UpdateCoverJob : Job
{
    [StringLength(64)] [Required] public string MangaId { get; init; }

    private Manga _manga = null!;
    
    [JsonIgnore]
    public Manga Manga 
    {
        get => LazyLoader.Load(this, ref _manga);
        init => _manga = value;
    }
    
    
    public UpdateCoverJob(Manga manga, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(UpdateCoverJob)), JobType.UpdateCoversJob, recurrenceMs, parentJob, dependsOnJobs)
    {
        this.MangaId = manga.MangaId;
        this.Manga = manga;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal UpdateCoverJob(ILazyLoader lazyLoader, string jobId, ulong recurrenceMs, string mangaId, string? parentJobId)
        : base(lazyLoader, jobId, JobType.UpdateCoversJob, recurrenceMs, parentJobId)
    {
        this.MangaId = mangaId;
    }

    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        bool keepCover = context.Jobs
            .Any(job => job.JobType == JobType.DownloadAvailableChaptersJob
                        && ((DownloadAvailableChaptersJob)job).MangaId == MangaId);
        if (!keepCover)
        {
            if(File.Exists(Manga.CoverFileNameInCache))
                File.Delete(Manga.CoverFileNameInCache);
            try
            {
                Manga.CoverFileNameInCache = null;
                context.Jobs.Remove(this);
                context.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                Log.Error(e);
            }
        }
        else
        {
            return [new DownloadMangaCoverJob(Manga, this)];
        }
        return [];
    }
}