using System.ComponentModel.DataAnnotations;
using API.Schema.MangaConnectors;
using Newtonsoft.Json;

namespace API.Schema.Jobs;

public class UpdateMetadataJob(ulong recurrenceMs, string mangaId, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(UpdateMetadataJob)), JobType.UpdateMetaDataJob, recurrenceMs, parentJobId, dependsOnJobsIds)
{
    [MaxLength(64)]
    public string MangaId { get; init; } = mangaId;
    
    [JsonIgnore]
    public virtual Manga? Manga { get; init; }
    
    /// <summary>
    /// Updates all data related to Manga.
    /// Retrieves data from Mangaconnector
    /// Updates Chapter-info
    /// </summary>
    /// <param name="context"></param>
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        throw new NotImplementedException();
    }
}