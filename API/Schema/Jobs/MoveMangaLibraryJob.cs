using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class MoveMangaLibraryJob : Job
{
    [StringLength(64)] [Required] public string MangaId { get; init; }
    [JsonIgnore] public Manga Manga { get; init; } = null!;
    [StringLength(64)] [Required] public string ToLibraryId { get; init; }
    public LocalLibrary ToLibrary { get; init; } = null!;
    
    public MoveMangaLibraryJob(Manga manga, LocalLibrary toLibrary, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(MoveMangaLibraryJob)), JobType.MoveMangaLibraryJob, 0, parentJob, dependsOnJobs)
    {
        this.MangaId = manga.MangaId;
        this.Manga = manga;
        this.ToLibraryId = toLibrary.LocalLibraryId;
        this.ToLibrary = toLibrary;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal MoveMangaLibraryJob(string mangaId, string toLibraryId, string? parentJobId)
        : base(TokenGen.CreateToken(typeof(MoveMangaLibraryJob)), JobType.MoveMangaLibraryJob, 0, parentJobId)
    {
        this.MangaId = mangaId;
        this.ToLibraryId = toLibraryId;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        Dictionary<Chapter, string> oldPath = Manga.Chapters.ToDictionary(c => c, c => c.FullArchiveFilePath);
        Manga.Library = ToLibrary;
        try
        {
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
            return [];
        }

        return Manga.Chapters.Select(c => new MoveFileOrFolderJob(oldPath[c], c.FullArchiveFilePath));
    }
}