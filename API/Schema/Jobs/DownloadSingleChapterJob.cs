using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Runtime.InteropServices;
using API.MangaDownloadClients;
using API.Schema.MangaConnectors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Binarization;
using static System.IO.UnixFileMode;

namespace API.Schema.Jobs;

public class DownloadSingleChapterJob(string chapterId, string? parentJobId = null, ICollection<string>? dependsOnJobsIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadSingleChapterJob)), JobType.DownloadSingleChapterJob, 0, parentJobId, dependsOnJobsIds)
{
    [StringLength(64)]
    [Required]
    public string ChapterId { get; init; } = chapterId;
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        Chapter? chapter = context.Chapters.Find(ChapterId);
        if (chapter is null)
        {
            Log.Error("Chapter is null.");
            return [];
        }
        Manga? manga = context.Mangas.Find(chapter.ParentMangaId) ?? chapter.ParentManga;
        if (manga is null)
        {
            Log.Error("Manga is null.");
            return [];
        }
        MangaConnector? connector = context.MangaConnectors.Find(manga.MangaConnectorId) ?? manga.MangaConnector;
        if (connector is null)
        {
            Log.Error("Connector is null.");
            return [];
        }
        string[] imageUrls = connector.GetChapterImageUrls(chapter);
        if (imageUrls.Length < 1)
        {
            Log.Info($"No imageUrls for chapter {ChapterId}");
            return [];
        }
        string? saveArchiveFilePath = chapter.FullArchiveFilePath;
        if (saveArchiveFilePath is null)
        {
            Log.Error("saveArchiveFilePath is null.");
            return [];
        }
        
        //Check if Publication Directory already exists
        string directoryPath = Path.GetDirectoryName(saveArchiveFilePath)!;
        if (!Directory.Exists(directoryPath))
        {
            Log.Info($"Creating publication Directory: {directoryPath}");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Directory.CreateDirectory(directoryPath,
                    UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute );
            else
                Directory.CreateDirectory(directoryPath);
        }

        if (File.Exists(saveArchiveFilePath)) //Don't download twice. Redownload
        {
            Log.Info($"Archive {saveArchiveFilePath} already existed, but deleting and re-downloading.");
            File.Delete(saveArchiveFilePath);
        }
        
        //Create a temporary folder to store images
        string tempFolder = Directory.CreateTempSubdirectory("trangatemp").FullName;
        Log.Debug($"Created temp folder: {tempFolder}");

        Log.Info($"Downloading images: {ChapterId}");
        int chapterNum = 0;
        //Download all Images to temporary Folder
        foreach (string imageUrl in imageUrls)
        {
            string extension = imageUrl.Split('.')[^1].Split('?')[0];
            string imagePath = Path.Join(tempFolder, $"{chapterNum++}.{extension}");
            bool status = DownloadImage(imageUrl, imagePath);
            if (status is false)
            {
                Log.Error($"Failed to download image: {imageUrl}");
                return [];
            }
        }
        
        CopyCoverFromCacheToDownloadLocation(manga);
        
        Log.Debug($"Creating ComicInfo.xml {ChapterId}");
        File.WriteAllText(Path.Join(tempFolder, "ComicInfo.xml"), chapter.GetComicInfoXmlString());
        
        Log.Debug($"Packaging images to archive {ChapterId}");
        //ZIP-it and ship-it
        ZipFile.CreateFromDirectory(tempFolder, saveArchiveFilePath);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(saveArchiveFilePath, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute | OtherRead | OtherExecute);
        Directory.Delete(tempFolder, true); //Cleanup
        
        chapter.Downloaded = true;
        context.SaveChanges();

        return [new UpdateFilesDownloadedJob(0, manga.MangaId, this.JobId)];
    }
    
    private void ProcessImage(string imagePath)
    {
        if (!TrangaSettings.bwImages && TrangaSettings.compression == 100)
        {
            Log.Debug($"No processing requested for image");
            return;
        }
        
        Log.Debug($"Processing image: {imagePath}");
        
        using Image image = Image.Load(imagePath);
        File.Delete(imagePath);
        if(TrangaSettings.bwImages) 
            image.Mutate(i => i.ApplyProcessor(new AdaptiveThresholdProcessor()));
        image.SaveAsJpeg(imagePath, new JpegEncoder()
        {
            Quality = TrangaSettings.compression
        });
    }
    
    private void CopyCoverFromCacheToDownloadLocation(Manga manga)
    {
        //Check if Publication already has a Folder and cover
        string publicationFolder = manga.CreatePublicationFolder();
        DirectoryInfo dirInfo = new (publicationFolder);
        if (dirInfo.EnumerateFiles().Any(info => info.Name.Contains("cover", StringComparison.InvariantCultureIgnoreCase)))
        {
            Log.Debug($"Cover already exists at {publicationFolder}");
            return;
        }

        Log.Info($"Copying cover to {publicationFolder}");
        string? fileInCache = manga.CoverFileNameInCache ?? manga.SaveCoverImageToCache();
        if (fileInCache is null)
        {
            Log.Error($"File {fileInCache} does not exist");
            return;
        }
        
        string newFilePath = Path.Join(publicationFolder, $"cover.{Path.GetFileName(fileInCache).Split('.')[^1]}" );
        File.Copy(fileInCache, newFilePath, true);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(newFilePath, GroupRead | GroupWrite | UserRead | UserWrite);
        Log.Debug($"Copied cover from {fileInCache} to {newFilePath}");
    }
    
    private bool DownloadImage(string imageUrl, string savePath)
    {
        HttpDownloadClient downloadClient = new();
        RequestResult requestResult = downloadClient.MakeRequest(imageUrl, RequestType.MangaImage);
        
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return false;
        if (requestResult.result == Stream.Null)
            return false;

        FileStream fs = new (savePath, FileMode.Create, FileAccess.Write, FileShare.None);
        requestResult.result.CopyTo(fs);
        fs.Close();
        ProcessImage(savePath);
        return true;
    }
}