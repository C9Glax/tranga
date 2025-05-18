using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class UpdateSingleChapterDownloadedJob : Job
{
    [StringLength(64)] [Required] public string ChapterId { get; init; }

    private Chapter _chapter = null!;
    
    [JsonIgnore]
    public Chapter Chapter 
    {
        get => LazyLoader.Load(this, ref _chapter);
        init => _chapter = value;
    }
    
    public UpdateSingleChapterDownloadedJob(Chapter chapter, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(UpdateSingleChapterDownloadedJob)), JobType.UpdateSingleChapterDownloadedJob, 0, parentJob, dependsOnJobs)
    {
        this.ChapterId = chapter.ChapterId;
        this.Chapter = chapter;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal UpdateSingleChapterDownloadedJob(ILazyLoader lazyLoader, string jobId, ulong recurrenceMs, string chapterId, string? parentJobId) 
        : base(lazyLoader, jobId, JobType.UpdateSingleChapterDownloadedJob, recurrenceMs, parentJobId)
    {
        this.ChapterId = chapterId;
    }

    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        context.Entry(Chapter).Reference<Manga>(c => c.ParentManga).Load();
        context.Entry(Chapter.ParentManga).Reference<LocalLibrary>(m => m.Library).Load();
        Chapter.Downloaded = Chapter.CheckDownloaded();

        try
        {
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
        }
        return [];
    }
}