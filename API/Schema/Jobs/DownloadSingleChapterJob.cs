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

public class DownloadSingleChapterJob(string chapterId, string? parentJobId = null, string[]? dependsOnJobIds = null)
    : Job(TokenGen.CreateToken(typeof(DownloadSingleChapterJob), 64), JobType.DownloadSingleChapterJob, 0, parentJobId, dependsOnJobIds)
{
    [MaxLength(64)]
    public string ChapterId { get; init; } = chapterId;
    public virtual Chapter Chapter { get; init; }
    
    public override IEnumerable<Job> Run()
    {
        MangaConnector connector = Chapter.ParentManga.MangaConnector;
        DownloadChapterImages(Chapter);
        return [];
    }
    
    private bool DownloadChapterImages(Chapter chapter)
    {
        MangaConnector connector = Chapter.ParentManga.MangaConnector;
        string[] imageUrls = connector.GetChapterImageUrls(Chapter);
        string saveArchiveFilePath = chapter.GetArchiveFilePath();
        
        //Check if Publication Directory already exists
        string directoryPath = Path.GetDirectoryName(saveArchiveFilePath)!;
        if (!Directory.Exists(directoryPath))
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Directory.CreateDirectory(directoryPath,
                    UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute );
            else
                Directory.CreateDirectory(directoryPath);

        if (File.Exists(saveArchiveFilePath)) //Don't download twice. Redownload
            File.Delete(saveArchiveFilePath);
        
        //Create a temporary folder to store images
        string tempFolder = Directory.CreateTempSubdirectory("trangatemp").FullName;

        int chapterNum = 0;
        //Download all Images to temporary Folder
        if (imageUrls.Length == 0)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                File.SetUnixFileMode(saveArchiveFilePath, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute);
            Directory.Delete(tempFolder, true);
            return false;
        }
        
        foreach (string imageUrl in imageUrls)
        {
            string extension = imageUrl.Split('.')[^1].Split('?')[0];
            string imagePath = Path.Join(tempFolder, $"{chapterNum++}.{extension}");
            bool status = DownloadImage(imageUrl, imagePath);
            if (status is false)
                return false;
        }

        CopyCoverFromCacheToDownloadLocation();
        
        File.WriteAllText(Path.Join(tempFolder, "ComicInfo.xml"), chapter.GetComicInfoXmlString());
        
        //ZIP-it and ship-it
        ZipFile.CreateFromDirectory(tempFolder, saveArchiveFilePath);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(saveArchiveFilePath, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute | OtherRead | OtherExecute);
        Directory.Delete(tempFolder, true); //Cleanup
        
        return true;
    }
    
    private void ProcessImage(string imagePath)
    {
        if (!TrangaSettings.bwImages && TrangaSettings.compression == 100)
            return;
        DateTime start = DateTime.Now;
        using Image image = Image.Load(imagePath);
        File.Delete(imagePath);
        if(TrangaSettings.bwImages) 
            image.Mutate(i => i.ApplyProcessor(new AdaptiveThresholdProcessor()));
        image.SaveAsJpeg(imagePath, new JpegEncoder()
        {
            Quality = TrangaSettings.compression
        });
    }
    
    private void CopyCoverFromCacheToDownloadLocation(int? retries = 1)
    {
        //Check if Publication already has a Folder and cover
        string publicationFolder = Chapter.ParentManga.CreatePublicationFolder();
        DirectoryInfo dirInfo = new (publicationFolder);
        if (dirInfo.EnumerateFiles().Any(info => info.Name.Contains("cover", StringComparison.InvariantCultureIgnoreCase)))
        {
            return;
        }

        string? fileInCache = Chapter.ParentManga.CoverFileNameInCache;
        if (fileInCache is null || !File.Exists(fileInCache))
        {
            if (retries > 0 && Chapter.ParentManga.CoverUrl is not null)
            {
                Chapter.ParentManga.SaveCoverImageToCache();
                CopyCoverFromCacheToDownloadLocation(--retries);
            }

            return;
        }
        string newFilePath = Path.Join(publicationFolder, $"cover.{Path.GetFileName(fileInCache).Split('.')[^1]}" );
        File.Copy(fileInCache, newFilePath, true);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(newFilePath, GroupRead | GroupWrite | UserRead | UserWrite);
    }
    
    private bool DownloadImage(string imageUrl, string savePath)
    {
        HttpDownloadClient downloadClient = new();
        RequestResult requestResult = downloadClient.MakeRequest(imageUrl, RequestType.MangaImage);
        
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return false;
        if (requestResult.result == Stream.Null)
            return false;

        FileStream fs = new (savePath, FileMode.Create);
        requestResult.result.CopyTo(fs);
        fs.Close();
        ProcessImage(savePath);
        return true;
    }
}