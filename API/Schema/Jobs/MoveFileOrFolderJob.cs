﻿namespace API.Schema.Jobs;

public class MoveFileOrFolderJob(string fromLocation, string toLocation, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(MoveFileOrFolderJob)), JobType.MoveFileOrFolderJob, 0, parentJobId, dependsOnJobsIds)
{
    public string FromLocation { get; init; } = fromLocation;
    public string ToLocation { get; init; } = toLocation;
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        try
        {
            FileInfo fi = new FileInfo(FromLocation);
            if (!fi.Exists)
                return [];
            if (File.Exists(ToLocation))//Do not override existing
                return [];
            if(fi.Attributes.HasFlag(FileAttributes.Directory))
                MoveDirectory(fi, ToLocation);
            else
                MoveFile(fi, ToLocation);
        }
        catch (Exception e)
        {
            
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