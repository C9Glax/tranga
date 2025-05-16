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
    internal DownloadAvailableChaptersJob(ILazyLoader lazyLoader, string mangaId, ulong recurrenceMs, string? parentJobId)
        : base(lazyLoader, TokenGen.CreateToken(typeof(DownloadAvailableChaptersJob)), JobType.DownloadAvailableChaptersJob, recurrenceMs, parentJobId)
    {
        this.MangaId = mangaId;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        context.Attach(Manga);
        return Manga.Chapters.Select(chapter => new DownloadSingleChapterJob(chapter, this));
    }
}