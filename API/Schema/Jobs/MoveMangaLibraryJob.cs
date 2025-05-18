using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class MoveMangaLibraryJob : Job
{
    [StringLength(64)] [Required] public string MangaId { get; init; }

    private Manga _manga = null!;
    
    [JsonIgnore]
    public Manga Manga 
    {
        get => LazyLoader.Load(this, ref _manga);
        init => _manga = value;
    }
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
    internal MoveMangaLibraryJob(ILazyLoader lazyLoader, string jobId, ulong recurrenceMs, string mangaId, string toLibraryId, string? parentJobId)
        : base(lazyLoader, jobId, JobType.MoveMangaLibraryJob, recurrenceMs, parentJobId)
    {
        this.MangaId = mangaId;
        this.ToLibraryId = toLibraryId;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        context.Entry(Manga).Reference<LocalLibrary>(m => m.Library).Load();
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