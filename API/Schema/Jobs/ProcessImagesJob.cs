namespace API.Schema.Jobs;

public class ProcessImagesJob(string path, bool bw, int compression, string? parentJobId = null, string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(ProcessImagesJob), 64), JobType.ProcessImagesJob, 0, parentJobId, dependsOnJobIds)
{
    public string Path { get; init; } = path;
    public bool Bw { get; init; } = bw;
    public int Compression { get; init; } = compression;
}