using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadNewChaptersJob(ulong recurrenceMs, string mangaId, string? parentJobId = null, string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadNewChaptersJob), 64), JobType.DownloadNewChaptersJob, recurrenceMs, parentJobId, dependsOnJobIds)
{
    [MaxLength(64)]
    public string MangaId { get; init; } = mangaId;
    public virtual Manga Manga { get; init; }
}