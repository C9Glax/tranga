using System.ComponentModel.DataAnnotations;

namespace API.Schema.Jobs;

public class UpdateMetadataJob(ulong recurrenceMs, string mangaId, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(UpdateMetadataJob), 64), JobType.UpdateMetaDataJob, recurrenceMs, parentJobId, dependsOnJobsIds)
{
    [MaxLength(64)]
    public string MangaId { get; init; } = mangaId;
    public virtual Manga Manga { get; init; }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        throw new NotImplementedException();
    }
}