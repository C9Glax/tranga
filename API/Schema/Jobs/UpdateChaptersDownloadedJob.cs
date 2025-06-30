using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class UpdateChaptersDownloadedJob : Job
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
    
    public UpdateChaptersDownloadedJob(Manga manga, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(UpdateChaptersDownloadedJob)), JobType.UpdateChaptersDownloadedJob, recurrenceMs, parentJob, dependsOnJobs)
    {
        this.Manga = manga;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal UpdateChaptersDownloadedJob(ILazyLoader lazyLoader, string key, ulong recurrenceMs, string mangaId, string? parentJobId)
        : base(lazyLoader, key, JobType.UpdateChaptersDownloadedJob, recurrenceMs, parentJobId)
    {
        this.MangaId = mangaId;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        context.Entry(Manga).Reference<FileLibrary>(m => m.Library).Load();
        foreach (Chapter mangaChapter in Manga.Chapters)
        {
            mangaChapter.Downloaded = mangaChapter.CheckDownloaded();
        }

        try
        {
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
        }
        return [];
    }
}