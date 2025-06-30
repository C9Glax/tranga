using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class DownloadMangaCoverJob : JobWithDownloading
{
    private MangaConnectorMangaEntry? _mangaConnectorMangaEntry = null!;
    [JsonIgnore]
    public MangaConnectorMangaEntry MangaConnectorMangaEntry
    {
        get => LazyLoader.Load(this, ref _mangaConnectorMangaEntry) ?? throw new InvalidOperationException();
        init => _mangaConnectorMangaEntry = value;
    }

    public DownloadMangaCoverJob(MangaConnectorMangaEntry mangaConnectorEntry, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(DownloadMangaCoverJob)), JobType.DownloadMangaCoverJob, 0, mangaConnectorEntry.MangaConnector, parentJob, dependsOnJobs)
    {
        this.MangaConnectorMangaEntry = mangaConnectorEntry;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal DownloadMangaCoverJob(ILazyLoader lazyLoader, string jobId, ulong recurrenceMs, string mangaConnectorName, string? parentJobId)
        : base(lazyLoader, jobId, JobType.DownloadMangaCoverJob, recurrenceMs, mangaConnectorName, parentJobId)
    {
        
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        try
        {
            MangaConnectorMangaEntry.Manga.CoverFileNameInCache = MangaConnectorMangaEntry.MangaConnector.SaveCoverImageToCache(MangaConnectorMangaEntry.Manga);
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
        }
        return [];
    }
}