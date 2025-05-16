using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class UpdateChaptersDownloadedJob : Job
{
    [StringLength(64)] [Required] public string MangaId { get; init; }
    [JsonIgnore] public Manga Manga { get; init; } = null!;
    
    public UpdateChaptersDownloadedJob(Manga manga, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(UpdateChaptersDownloadedJob)), JobType.UpdateChaptersDownloadedJob, recurrenceMs, parentJob, dependsOnJobs)
    {
        this.MangaId = manga.MangaId;
        this.Manga = manga;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal UpdateChaptersDownloadedJob(string mangaId, ulong recurrenceMs, string? parentJobId)
        : base(TokenGen.CreateToken(typeof(UpdateChaptersDownloadedJob)), JobType.UpdateChaptersDownloadedJob, recurrenceMs, parentJobId)
    {
        this.MangaId = mangaId;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        context.Attach(Manga);
        return Manga.Chapters.Select(c => new UpdateSingleChapterDownloadedJob(c, this));
    }
}