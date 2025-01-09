namespace API.Schema.Jobs;

public class MoveFileOrFolderJob(string fromLocation, string toLocation, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(MoveFileOrFolderJob), 64), JobType.MoveFileOrFolderJob, 0, parentJobId, dependsOnJobsIds)
{
    public string FromLocation { get; init; } = fromLocation;
    public string ToLocation { get; init; } = toLocation;
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        throw new NotImplementedException();
    }
}