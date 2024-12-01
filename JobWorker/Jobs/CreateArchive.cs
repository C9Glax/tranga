using System.IO.Compression;
using System.Runtime.InteropServices;
using API.Schema.Jobs;
using static System.IO.UnixFileMode;

namespace JobWorker.Jobs;

public class CreateArchive(CreateArchiveJob data) : Job<CreateArchiveJob>(data)
{
    protected override IEnumerable<Job> ExecuteReturnSubTasksInternal(CreateArchiveJob data)
    {
        Job? ci = data.DependsOnJobs.FirstOrDefault(j => j is CreateComicInfoXmlJob);
        if (ci is null)
        {
            Log.Error($"{this} failed: CreateComicInfoXmlJob is missing.");
            return [];
        }

        Job? dc = data.DependsOnJobs.FirstOrDefault(j => j is DownloadSingleChapterJob);
        if (dc is null)
        {
            Log.Error($"{this} failed: DownloadSingleChapterJob is missing.");
            return [];
        }

        string comicInfoXmlPath = (string)(ci as CreateComicInfoXmlJob)!.returnValue!;
        string pagesPath = (string)(dc as DownloadSingleChapterJob)!.returnValue!;
        
        File.Move(comicInfoXmlPath, pagesPath);
        
        ZipFile.CreateFromDirectory(pagesPath, data.Chapter.ArchiveFileName);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(data.Chapter.ArchiveFileName, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute);
        Directory.Delete(pagesPath, true); //Cleanup

        return [];
    }
}