using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadAvailableChaptersJob : Job
{
    [StringLength(64)] [Required] public string MangaId { get; init; }

    private Manga _manga = null!;
    
    [JsonIgnore]
    public Manga Manga 
    {
        get => LazyLoader.Load(this, ref _manga);
        init => _manga = value;
    }
    
    public DownloadAvailableChaptersJob(Manga manga, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(DownloadAvailableChaptersJob)), JobType.DownloadAvailableChaptersJob, recurrenceMs, parentJob, dependsOnJobs)
    {
        this.MangaId = manga.MangaId;
        this.Manga = manga;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal DownloadAvailableChaptersJob(ILazyLoader lazyLoader, string jobId, ulong recurrenceMs, string mangaId, string? parentJobId)
        : base(lazyLoader, jobId, JobType.DownloadAvailableChaptersJob, recurrenceMs, parentJobId)
    {
        this.MangaId = mangaId;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        context.Entry(Manga).Reference<LocalLibrary>(m => m.Library).Load();
        return Manga.Chapters.Where(c => c.Downloaded == false).Select(chapter => new DownloadSingleChapterJob(chapter, this));
    }
}