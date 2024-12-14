using System.ComponentModel.DataAnnotations;

namespace API.Schema.Jobs;

public class UpdateMetadataJob(ulong recurrenceMs, string mangaId, string? parentJobId = null, string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(UpdateMetadataJob), 64), JobType.UpdateMetaDataJob, recurrenceMs, parentJobId, dependsOnJobIds)
{
    [MaxLength(64)]
    public string MangaId { get; init; } = mangaId;
    public virtual Manga Manga { get; init; }
    
    public override IEnumerable<Job> Run()
    {
        throw new NotImplementedException();
    }
}