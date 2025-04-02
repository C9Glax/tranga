using System.ComponentModel.DataAnnotations;
using API.Schema.MangaConnectors;
using Microsoft.EntityFrameworkCore;
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
        Manga? manga = context.Mangas.Find(MangaId) ?? Manga;
        if (manga is null)
        {
            Log.Error("Manga is null.");
            return [];
        }
        MangaConnector? connector = manga.MangaConnector ?? context.MangaConnectors.Find(manga.MangaConnectorId);
        if (connector is null)
        {
            Log.Error("Connector is null.");
            return [];
        }
        // This gets all chapters that are not downloaded
        Chapter[] allNewChapters = connector.GetNewChapters(manga).DistinctBy(c => c.ChapterId).ToArray();
        Log.Info($"{allNewChapters.Length} new chapters.");

        try
        {
            // This filters out chapters that are not downloaded but already exist in the DB
            string[] chapterIds = context.Chapters.Where(chapter => chapter.ParentMangaId == manga.MangaId)
                .Select(chapter => chapter.ChapterId).ToArray();
            Chapter[] newChapters = allNewChapters.Where(chapter => !chapterIds.Contains(chapter.ChapterId)).ToArray();
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