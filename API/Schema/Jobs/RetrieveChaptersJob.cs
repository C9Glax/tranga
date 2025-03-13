using System.ComponentModel.DataAnnotations;
using API.Schema.MangaConnectors;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class RetrieveChaptersJob(ulong recurrenceMs, string mangaId, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(RetrieveChaptersJob)), JobType.RetrieveChaptersJob, recurrenceMs, parentJobId, dependsOnJobsIds)
{
    [StringLength(64)]
    [Required]
    public string MangaId { get; init; } = mangaId;
    
    [JsonIgnore]
    public Manga? Manga { get; init; }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        /*
         * For some reason, directly using Manga from above instead of finding it again causes DBContext to consider
         * Manga as a new entity and Postgres throws a Duplicate PK exception.
         * m.MangaConnector does not have this issue (IDK why).
         */ 
        Manga m = context.Manga.Find(MangaId)!; 
        MangaConnector connector = context.MangaConnectors.Find(m.MangaConnectorId)!;
        // This gets all chapters that are not downloaded
        Chapter[] allNewChapters = connector.GetNewChapters(m).DistinctBy(c => c.ChapterId).ToArray();
        
        // This filters out chapters that are not downloaded but already exist in the DB
        string[] chapterIds = context.Chapters.Where(chapter => chapter.ParentMangaId == m.MangaId).Select(chapter => chapter.ChapterId).ToArray();
        Chapter[] newChapters = allNewChapters.Where(chapter => !chapterIds.Contains(chapter.ChapterId)).ToArray();
        context.Chapters.AddRange(newChapters);
        context.SaveChanges();

        return [];
    }
}