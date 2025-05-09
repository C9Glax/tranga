using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadMangaCoverJob : Job
{
    [StringLength(64)] [Required] public string MangaId { get; init; }
    [JsonIgnore] public Manga Manga { get; init; } = null!;

    public DownloadMangaCoverJob(Manga manga, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(DownloadMangaCoverJob)), JobType.DownloadMangaCoverJob, 0, parentJob, dependsOnJobs)
    {
        this.MangaId = manga.MangaId;
        this.Manga = manga;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    public DownloadMangaCoverJob(string mangaId, string? parentJobId = null)
        : base(TokenGen.CreateToken(typeof(DownloadMangaCoverJob)), JobType.DownloadMangaCoverJob, 0, parentJobId)
    {
        this.MangaId = mangaId;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        try
        {
            Manga.CoverFileNameInCache = Manga.MangaConnector.SaveCoverImageToCache(Manga);
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
        }
        return [];
    }
}