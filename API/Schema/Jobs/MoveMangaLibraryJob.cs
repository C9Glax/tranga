using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.Jobs;

public class MoveMangaLibraryJob(string mangaId, string toLibraryId, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(MoveMangaLibraryJob)), JobType.MoveMangaLibraryJob, 0, parentJobId, dependsOnJobsIds)
{
    [StringLength(64)]
    [Required]
    public string MangaId { get; init; } = mangaId;
    [StringLength(64)]
    [Required]
    public string ToLibraryId { get; init; } = toLibraryId;
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        Manga? manga = context.Mangas.Find(MangaId);
        if (manga is null)
        {
            Log.Error("Manga not found");
            return [];
        }
        LocalLibrary? library = context.LocalLibraries.Find(ToLibraryId);
        if (library is null)
        {
            Log.Error("LocalLibrary not found");
            return [];
        }
        Chapter[] chapters = context.Chapters.Where(c => c.ParentMangaId == MangaId).ToArray();
        Dictionary<Chapter, string> oldPath = chapters.ToDictionary(c => c, c => c.FullArchiveFilePath!);
        manga.Library = library;
        try
        {
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
            return [];
        }

        return chapters.Select(c => new MoveFileOrFolderJob(oldPath[c], c.FullArchiveFilePath!));
    }
}