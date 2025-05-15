using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class UpdateFilesDownloadedJob : Job
{
    [StringLength(64)] [Required] public string MangaId { get; init; }
    [JsonIgnore] public Manga Manga { get; init; } = null!;
    
    public UpdateFilesDownloadedJob(Manga manga, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(UpdateFilesDownloadedJob)), JobType.UpdateFilesDownloadedJob, recurrenceMs, parentJob, dependsOnJobs)
    {
        this.MangaId = manga.MangaId;
        this.Manga = manga;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal UpdateFilesDownloadedJob(string mangaId, ulong recurrenceMs, string? parentJobId)
        : base(TokenGen.CreateToken(typeof(UpdateFilesDownloadedJob)), JobType.UpdateFilesDownloadedJob, recurrenceMs, parentJobId)
    {
        this.MangaId = mangaId;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        context.Attach(Manga);
        foreach (Chapter chapter in Manga.Chapters)
            chapter.Downloaded = chapter.CheckDownloaded();

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