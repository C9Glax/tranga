using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class RetrieveChaptersJob : Job
{
    [StringLength(64)] [Required] public string MangaId { get; init; }
    [JsonIgnore] public Manga Manga { get; init; } = null!;
    [StringLength(8)] [Required] public string Language { get; private set; }
    
    public RetrieveChaptersJob(Manga manga, string language, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(RetrieveChaptersJob)), JobType.RetrieveChaptersJob, recurrenceMs, parentJob, dependsOnJobs)
    {
        this.MangaId = manga.MangaId;
        this.Manga = manga;
        this.Language = language;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal RetrieveChaptersJob(string mangaId, string language, ulong recurrenceMs, string? parentJobId)
        : base(TokenGen.CreateToken(typeof(RetrieveChaptersJob)), JobType.RetrieveChaptersJob, recurrenceMs, parentJobId)
    {
        this.MangaId = mangaId;
        this.Language = language;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        context.Attach(Manga);
        // This gets all chapters that are not downloaded
        Chapter[] allChapters = Manga.MangaConnector.GetChapters(Manga, Language);
        Chapter[] newChapters = allChapters.Where(chapter => context.Chapters.Contains(chapter) == false).ToArray();
        Log.Info($"{newChapters.Length} new chapters.");

        try
        {
            context.Chapters.AddRange(newChapters);
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
        }

        return [];
    }
}