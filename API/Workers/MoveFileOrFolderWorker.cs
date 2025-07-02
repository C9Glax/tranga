namespace API.Workers;

public class MoveFileOrFolderWorker(string toLocation, string fromLocation, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorker(dependsOn)
{
    public readonly string FromLocation = fromLocation;
    public readonly string ToLocation = toLocation;

    protected override BaseWorker[] DoWorkInternal()
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