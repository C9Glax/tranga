using API.Schema.Jobs;

namespace JobWorker.Jobs;

public class MoveFileOrFolder(MoveFileOrFolderJob data): Job<MoveFileOrFolderJob>(data)
{
    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal(MoveFileOrFolderJob data)
    {
        string from = data.FromLocation;
        string to = data.ToLocation;
        
        FileAttributes attr = File.GetAttributes(from);

        switch ((attr & FileAttributes.Directory) != 0)
        {
            case true: Directory.Move(from, to);
                break;
            case false: File.Move(from, to, true);
                break;
        }

        return [];
    }
}