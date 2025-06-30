using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class RetrieveChaptersJob : JobWithDownloading
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
    
    [StringLength(8)] [Required] public string Language { get; private set; }
    
    public RetrieveChaptersJob(Manga manga, string language, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(RetrieveChaptersJob)), JobType.RetrieveChaptersJob, recurrenceMs, parentJob, dependsOnJobs)
    {
        this.Manga = manga;
        this.Language = language;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal RetrieveChaptersJob(ILazyLoader lazyLoader, string key, string mangaId, ulong recurrenceMs, string language, string? parentJobId)
        : base(lazyLoader, key, JobType.RetrieveChaptersJob, recurrenceMs, parentJobId)
    {
        this.MangaId = mangaId;
        this.Language = language;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        //TODO MangaConnector Selection
        MangaConnectorId<Manga> mcId = Manga.MangaConnectorIds.First();
        
        // This gets all chapters that are not downloaded
        (Chapter, MangaConnectorId<Chapter>)[] allChapters = mcId.MangaConnector.GetChapters(mcId, Language).DistinctBy(c => c.Item1.Key).ToArray();
        (Chapter, MangaConnectorId<Chapter>)[] newChapters = allChapters.Where(chapter => Manga.Chapters.Any(ch => chapter.Item1.Key == ch.Key && ch.Downloaded) == false).ToArray();
        Log.Info($"{Manga.Chapters.Count} existing + {newChapters.Length} new chapters.");

        try
        {
            foreach ((Chapter chapter, MangaConnectorId<Chapter> mcId) newChapter in newChapters)
            {
                Manga.Chapters.Add(newChapter.chapter);
                context.MangaConnectorToChapter.Add(newChapter.mcId);
            }
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
        }

        return [];
    }
}