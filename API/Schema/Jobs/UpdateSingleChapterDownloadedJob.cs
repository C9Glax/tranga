using System.ComponentModel.DataAnnotations;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class UpdateSingleChapterDownloadedJob : Job
{
    [StringLength(64)] [Required] public string ChapterId { get; init; }
    [JsonIgnore] public Chapter Chapter { get; init; } = null!;
    
    public UpdateSingleChapterDownloadedJob(Chapter chapter, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(UpdateSingleChapterDownloadedJob)), JobType.UpdateSingleChapterDownloadedJob, 0, parentJob, dependsOnJobs)
    {
        this.ChapterId = chapter.ChapterId;
        this.Chapter = chapter;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal UpdateSingleChapterDownloadedJob(string chapterId, string? parentJobId) 
        : base(TokenGen.CreateToken(typeof(UpdateSingleChapterDownloadedJob)), JobType.UpdateSingleChapterDownloadedJob, 0, parentJobId)
    {
        this.ChapterId = chapterId;
    }

    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        context.Attach(Chapter);
        Chapter.Downloaded = Chapter.CheckDownloaded();
        context.SaveChanges();

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