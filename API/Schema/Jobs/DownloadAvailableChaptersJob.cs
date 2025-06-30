using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadAvailableChaptersJob : JobWithDownloading
{
    private MangaConnectorMangaEntry? _mangaConnectorMangaEntry = null!;
    [JsonIgnore]
    public MangaConnectorMangaEntry MangaConnectorMangaEntry
    {
        get => LazyLoader.Load(this, ref _mangaConnectorMangaEntry) ?? throw new InvalidOperationException();
        init => _mangaConnectorMangaEntry = value;
    }
    
    public DownloadAvailableChaptersJob(MangaConnectorMangaEntry mangaConnectorMangaEntry, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(DownloadAvailableChaptersJob)), JobType.DownloadAvailableChaptersJob, recurrenceMs, mangaConnectorMangaEntry.MangaConnector, parentJob, dependsOnJobs)
    {
        this.MangaConnectorMangaEntry = mangaConnectorMangaEntry;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal DownloadAvailableChaptersJob(ILazyLoader lazyLoader, string jobId, ulong recurrenceMs, string mangaConnectorName, string? parentJobId)
        : base(lazyLoader, jobId, JobType.DownloadAvailableChaptersJob, recurrenceMs, mangaConnectorName, parentJobId)
    {
        
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        return MangaConnectorMangaEntry.Manga.Chapters.Where(c => c.Downloaded == false).Select(chapter => new DownloadSingleChapterJob(chapter, this.MangaConnectorMangaEntry));
    }
}