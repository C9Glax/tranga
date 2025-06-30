using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class MoveMangaLibraryJob : Job
{
    [StringLength(64)] [Required] public string MangaId { get; init; } = null!;
    private Manga? _manga;

    [JsonIgnore]
    public Manga Manga
    {
        get => LazyLoader.Load(this, ref _manga) ?? throw new InvalidOperationException();
        init
        {
            MangaId = value.Key;
            _manga = value;
        }
    }

    [StringLength(64)] [Required] public string ToLibraryId { get; private set; } = null!;
    private FileLibrary? _toFileLibrary;
    [JsonIgnore]
    public FileLibrary ToFileLibrary
    {
        get => LazyLoader.Load(this, ref _toFileLibrary) ?? throw new InvalidOperationException();
        init
        {
            ToLibraryId = value.Key;
            _toFileLibrary = value;
        }
    }

    public MoveMangaLibraryJob(Manga manga, FileLibrary toFileLibrary, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(MoveMangaLibraryJob)), JobType.MoveMangaLibraryJob, 0, parentJob, dependsOnJobs)
    {
        this.Manga = manga;
        this.ToFileLibrary = toFileLibrary;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal MoveMangaLibraryJob(ILazyLoader lazyLoader, string key, ulong recurrenceMs, string mangaId, string toLibraryId, string? parentJobId)
        : base(lazyLoader, key, JobType.MoveMangaLibraryJob, recurrenceMs, parentJobId)
    {
        this.MangaId = mangaId;
        this.ToLibraryId = toLibraryId;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        context.Entry(Manga).Reference<FileLibrary>(m => m.Library).Load();
        Dictionary<Chapter, string> oldPath = Manga.Chapters.ToDictionary(c => c, c => c.FullArchiveFilePath);
        Manga.Library = ToFileLibrary;
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