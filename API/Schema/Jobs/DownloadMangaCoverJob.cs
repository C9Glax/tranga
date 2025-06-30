using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadMangaCoverJob : JobWithDownloading
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
    
    public DownloadMangaCoverJob(Manga manga, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(DownloadMangaCoverJob)), JobType.DownloadMangaCoverJob, 0, parentJob, dependsOnJobs)
    {
        this.Manga = manga;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal DownloadMangaCoverJob(ILazyLoader lazyLoader, string key, string mangaId, ulong recurrenceMs, string? parentJobId)
        : base(lazyLoader, key, JobType.DownloadMangaCoverJob, recurrenceMs, parentJobId)
    {
        this.MangaId = mangaId;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        //TODO MangaConnector Selection
        MangaConnectorId<Manga> mcId = Manga.MangaConnectorIds.First();
        try
        {
            Manga.CoverFileNameInCache = mcId.MangaConnector.SaveCoverImageToCache(mcId);
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
        }
        return [];
    }
}