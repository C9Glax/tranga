using System.Diagnostics.CodeAnalysis;
using API.Schema.ActionsContext;
using API.Schema.ActionsContext.Actions;

namespace API.Workers;

public class MoveFileOrFolderWorker(string toLocation, string fromLocation, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContexts(dependsOn)
{
    public readonly string FromLocation = fromLocation;
    public readonly string ToLocation = toLocation;
    
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private ActionsContext ActionsContext = null!;

    protected override void SetContexts(IServiceScope serviceScope)
    {
        ActionsContext = GetContext<ActionsContext>(serviceScope);
    }

    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        try
        {
            FileInfo fi = new (FromLocation);
            if (!fi.Exists)
            {
                Log.ErrorFormat("File does not exist at {0}", FromLocation);
                return [];
            }

            if (File.Exists(ToLocation))//Do not override existing
            {
                Log.ErrorFormat("File already exists at {0}", ToLocation);
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

        ActionsContext.Actions.Add(new DataMovedActionRecord(FromLocation, ToLocation));
        if(await ActionsContext.Sync(CancellationToken, GetType(), "Library Moved") is { success: false } actionsContextException)
            Log.ErrorFormat("Failed to save database changes: {0}", actionsContextException.exceptionMessage);

        return [];
    }

    private static void MoveDirectory(FileInfo from, string toLocation)
    {
        Directory.Move(from.FullName, toLocation);        
    }

    private static void MoveFile(FileInfo from, string toLocation)
    {
        File.Move(from.FullName, toLocation);
    }

    public override string ToString() => $"{base.ToString()} {FromLocation} {ToLocation}";
}