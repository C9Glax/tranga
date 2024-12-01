using System.IO.Compression;
using System.Runtime.InteropServices;
using API.Schema.Jobs;
using static System.IO.UnixFileMode;

namespace JobWorker.Jobs;

public class CreateArchive(CreateArchiveJob data) : Job<CreateArchiveJob>(data)
{
    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal(CreateArchiveJob data)
    {
        File.Move(data.ComicInfoLocation, data.ImagesLocation);
        
        ZipFile.CreateFromDirectory(data.ImagesLocation, data.Chapter.ArchiveFileName);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(data.Chapter.ArchiveFileName, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute);
        Directory.Delete(data.ImagesLocation, true); //Cleanup

        return [];
    }
}