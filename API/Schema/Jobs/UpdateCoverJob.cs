using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class UpdateCoverJob : Job
{
    private MangaConnectorMangaEntry? _mangaConnectorMangaEntry = null!;
    [JsonIgnore]
    public MangaConnectorMangaEntry MangaConnectorMangaEntry
    {
        get => LazyLoader.Load(this, ref _mangaConnectorMangaEntry) ?? throw new InvalidOperationException();
        init => _mangaConnectorMangaEntry = value;
    }
    
    
    public UpdateCoverJob(MangaConnectorMangaEntry mangaConnectorMangaEntry, ulong recurrenceMs, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(UpdateCoverJob)), JobType.UpdateCoverJob, recurrenceMs, parentJob, dependsOnJobs)
    {
        this.MangaConnectorMangaEntry = mangaConnectorMangaEntry;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal UpdateCoverJob(ILazyLoader lazyLoader, string jobId, ulong recurrenceMs, string? parentJobId)
        : base(lazyLoader, jobId, JobType.UpdateCoverJob, recurrenceMs, parentJobId)
    {
    }

    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        bool keepCover = context.Jobs
            .Any(job => job.JobType == JobType.DownloadAvailableChaptersJob
                        && ((DownloadAvailableChaptersJob)job).MangaConnectorMangaEntry.MangaId == MangaConnectorMangaEntry.MangaId);
        if (!keepCover)
        {
            if(File.Exists(MangaConnectorMangaEntry.Manga.CoverFileNameInCache))
                File.Delete(MangaConnectorMangaEntry.Manga.CoverFileNameInCache);
            try
            {
                MangaConnectorMangaEntry.Manga.CoverFileNameInCache = null;
                context.Jobs.Remove(this);
                context.SaveChanges();
            }
            catch (DbUpdateException e)
            {
                Log.Error(e);
            }
        }
        else
        {
            return [new DownloadMangaCoverJob(MangaConnectorMangaEntry, this)];
        }
        return [];
    }
}