using API.Schema.Jobs;

namespace JobWorker.Jobs;

public class MoveFileOrFolder : Job<(string, string), object?>
{
    protected override (IEnumerable<Job>, object?) ExecuteReturnSubTasksInternal((string, string) data, Job[] relatedJobs)
    {
        string from = data.Item1;
        string to = data.Item2;
        
        FileAttributes attr = File.GetAttributes(from);

        switch ((attr & FileAttributes.Directory) != 0)
        {
            case true: Directory.Move(from, to);
                break;
            case false: File.Move(from, to, true);
                break;
        }

        return ([], null);
    }
}