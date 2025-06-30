using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadAvailableChaptersJob : JobWithDownloading
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
    
    public DownloadAvailableChaptersJob(Manga manga, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(DownloadAvailableChaptersJob)), JobType.DownloadAvailableChaptersJob, recurrenceMs, parentJob, dependsOnJobs)
    {
        this.Manga = manga;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal DownloadAvailableChaptersJob(ILazyLoader lazyLoader, string key, string mangaId, ulong recurrenceMs, string? parentJobId)
        : base(lazyLoader, key, JobType.DownloadAvailableChaptersJob, recurrenceMs, parentJobId)
    {
        this.MangaId = mangaId;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        // Chapters that aren't downloaded and for which no downloading-Job exists
        IEnumerable<Chapter> newChapters = Manga.Chapters
            .Where(c =>
                c.Downloaded == false &&
                context.Jobs.Any(j =>
                    j.JobType == JobType.DownloadSingleChapterJob &&
                    ((DownloadSingleChapterJob)j).Chapter.ParentMangaId == MangaId) == false);
        return newChapters.Select(c => new DownloadSingleChapterJob(c, this));
    }
}