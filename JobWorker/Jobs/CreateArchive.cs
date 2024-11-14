using API.Schema;
using API.Schema.Jobs;

namespace JobWorker.Jobs;

public class CreateArchive : Job<Chapter, string>
{
    protected override (IEnumerable<Job>, string) ExecuteReturnSubTasksInternal(Chapter data)
    {
        
    }
}