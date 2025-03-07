using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Runtime.InteropServices;
using API.MangaDownloadClients;
using API.Schema.MangaConnectors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Binarization;
using static System.IO.UnixFileMode;

namespace API.Schema.Jobs;

public class DownloadMangaCoverJob(string mangaId, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadMangaCoverJob)), JobType.DownloadMangaCoverJob, 0, parentJobId, dependsOnJobsIds)
{
    [MaxLength(64)]
    public string MangaId { get; init; } = mangaId;
    public Manga? Manga { get; init; }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        Manga? manga = Manga ?? context.Manga.Find(this.MangaId);
        if (manga is null)
            return [];
        
        manga.CoverFileNameInCache = manga.SaveCoverImageToCache();
        context.SaveChanges();
        return [];
    }
}