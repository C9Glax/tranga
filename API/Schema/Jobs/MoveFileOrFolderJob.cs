using System.ComponentModel.DataAnnotations;

namespace API.Schema.Jobs;

public class MoveFileOrFolderJob : Job
{
    [StringLength(256)]
    [Required]
    public string FromLocation { get; init; }
    [StringLength(256)]
    [Required]
    public string ToLocation { get; init; }
    
    public MoveFileOrFolderJob(string fromLocation, string toLocation, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(MoveFileOrFolderJob)), JobType.MoveFileOrFolderJob, 0, parentJob, dependsOnJobs)
    {
        this.FromLocation = fromLocation;
        this.ToLocation = toLocation;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal MoveFileOrFolderJob(string jobId, string fromLocation, string toLocation, string? parentJobId)
        : base(jobId, JobType.MoveFileOrFolderJob, 0, parentJobId)
    {
        this.FromLocation = fromLocation;
        this.ToLocation = toLocation;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        try
        {
            FileInfo fi = new (FromLocation);
            if (!fi.Exists)
            {
                Log.Error($"File does not exist at {FromLocation}");
                return [];
            }

            if (File.Exists(ToLocation))//Do not override existing
            {
                Log.Error($"File already exists at {ToLocation}");
                return [];
            } 
            if(fi.Attributes.HasFlag(FileAttributes.Directory))
                MoveDirectory(fi, ToLocation);
            else
                MoveFile(fi, ToLocation);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        return [];
    }

    private void MoveDirectory(FileInfo from, string toLocation)
    {
        Directory.Move(from.FullName, toLocation);        
    }

    private void MoveFile(FileInfo from, string toLocation)
    {
        File.Move(from.FullName, toLocation);
    }
}