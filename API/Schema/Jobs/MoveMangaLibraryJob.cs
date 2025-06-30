using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class MoveMangaLibraryJob : Job
{
    [StringLength(64)] [Required] public string MangaId { get; init; }

    private Manga? _manga = null!;
    
    [JsonIgnore]
    public Manga Manga 
    {
        get => LazyLoader.Load(this, ref _manga) ?? throw new InvalidOperationException();
        init => _manga = value;
    }

    [StringLength(64)] [Required] public string ToLibraryId { get; private set; } = null!;
    private LocalLibrary? _toLibrary = null!;
    [JsonIgnore]
    public LocalLibrary ToLibrary
    {
        get => LazyLoader.Load(this, ref _toLibrary) ?? throw new InvalidOperationException();
        init
        {
            ToLibraryId = value.LocalLibraryId;
            _toLibrary = value;
        }
    }

    public MoveMangaLibraryJob(Manga manga, LocalLibrary toLibrary, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(MoveMangaLibraryJob)), JobType.MoveMangaLibraryJob, 0, parentJob, dependsOnJobs)
    {
        this.MangaId = manga.MangaId;
        this.Manga = manga;
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