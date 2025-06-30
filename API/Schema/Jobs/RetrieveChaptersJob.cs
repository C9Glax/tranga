using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class RetrieveChaptersJob : JobWithDownloading
{
    private MangaConnectorMangaEntry? _mangaConnectorMangaEntry = null!;
    [JsonIgnore]
    public MangaConnectorMangaEntry MangaConnectorMangaEntry
    {
        get => LazyLoader.Load(this, ref _mangaConnectorMangaEntry) ?? throw new InvalidOperationException();
        init => _mangaConnectorMangaEntry = value;
    }
    
    [StringLength(8)] [Required] public string Language { get; private set; }
    
    public RetrieveChaptersJob(MangaConnectorMangaEntry mangaConnectorMangaEntry, string language, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(RetrieveChaptersJob)), JobType.RetrieveChaptersJob, recurrenceMs, mangaConnectorMangaEntry.MangaConnector, parentJob, dependsOnJobs)
    {
        this.MangaConnectorMangaEntry = mangaConnectorMangaEntry;
        this.Language = language;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal RetrieveChaptersJob(ILazyLoader lazyLoader, string jobId, ulong recurrenceMs, string mangaConnectorName, string language, string? parentJobId)
        : base(lazyLoader, jobId, JobType.RetrieveChaptersJob, recurrenceMs, mangaConnectorName, parentJobId)
    {
        this.Language = language;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        // This gets all chapters that are not downloaded
        Chapter[] allChapters = MangaConnectorMangaEntry.MangaConnector.GetChapters(MangaConnectorMangaEntry, Language).DistinctBy(c => c.ChapterId).ToArray();
        Chapter[] newChapters = allChapters.Where(chapter => MangaConnectorMangaEntry.Manga.Chapters.Select(c => c.ChapterId).Contains(chapter.ChapterId) == false).ToArray();
        Log.Info($"{MangaConnectorMangaEntry.Manga.Chapters.Count} existing + {newChapters.Length} new chapters.");

        try
        {
            foreach (Chapter newChapter in newChapters)
                MangaConnectorMangaEntry.Manga.Chapters.Add(newChapter);
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
        }

        return [];
    }
}