using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class UpdateCoverJob : Job
{
    [StringLength(64)] [Required] public string MangaId { get; init; } = null!;
    private Manga? _manga;

    [JsonIgnore]
    public Manga Manga
    {
        get => LazyLoader.Load(this, ref _manga) ?? throw new InvalidOperationException();
        init
        {
            MangaId = value.Key;
            _manga = value;
        }
    }
    
    
    public UpdateCoverJob(Manga manga, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(UpdateCoverJob)), JobType.UpdateCoverJob, recurrenceMs, parentJob, dependsOnJobs)
    {
        this.Manga = manga;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal UpdateCoverJob(ILazyLoader lazyLoader, string key, string mangaId, ulong recurrenceMs, string? parentJobId)
        : base(lazyLoader, key, JobType.UpdateCoverJob, recurrenceMs, parentJobId)
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