using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadMangaCoverJob(string mangaId, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadMangaCoverJob)), JobType.DownloadMangaCoverJob, 0, parentJobId, dependsOnJobsIds)
{
    [StringLength(64)]
    [Required]
    public string MangaId { get; init; } = mangaId;
    [JsonIgnore]
    public Manga? Manga { get; init; }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        Manga? manga = Manga ?? context.Mangas.Find(this.MangaId);
        if (manga is null)
        {
            Log.Error($"Manga {this.MangaId} not found.");
            return [];
        }
        
        manga.CoverFileNameInCache = manga.SaveCoverImageToCache();
        context.SaveChanges();
        Log.Info($"Saved cover for Manga {this.MangaId} to cache at {manga.CoverFileNameInCache}.");
        return [];
    }
}