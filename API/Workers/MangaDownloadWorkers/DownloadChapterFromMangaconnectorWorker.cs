using System.IO.Compression;
using System.Runtime.InteropServices;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Binarization;
using static System.IO.UnixFileMode;

namespace API.Workers;

public class DownloadChapterFromMangaconnectorWorker(Chapter chapter, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    protected override BaseWorker[] DoWorkInternal()
    {
        if (chapter.Downloaded)
        {
            Log.Info("Chapter was already downloaded.");
            return [];
        }
        
        //TODO MangaConnector Selection
        MangaConnectorId<Chapter> mcId = chapter.MangaConnectorIds.First();
        
        string[] imageUrls = mcId.MangaConnector.GetChapterImageUrls(mcId);
        if (imageUrls.Length < 1)
        {
            Log.Info($"No imageUrls for chapter {chapter}");
            return [];
        }
        string saveArchiveFilePath = chapter.FullArchiveFilePath;
        Log.Debug($"Chapter path: {saveArchiveFilePath}");
        
        //Check if Publication Directory already exists
        string? directoryPath = Path.GetDirectoryName(saveArchiveFilePath);
        if (directoryPath is null)
        {
            Log.Error($"Directory path could not be found: {saveArchiveFilePath}");
            this.Fail();
            return [];
        }
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

        Log.Info($"Downloading images: {chapter}");
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
        
        CopyCoverFromCacheToDownloadLocation(chapter.ParentManga);
        
        Log.Debug($"Creating ComicInfo.xml {chapter}");
        File.WriteAllText(Path.Join(tempFolder, "ComicInfo.xml"), chapter.GetComicInfoXmlString());
        
        Log.Debug($"Packaging images to archive {chapter}");
        //ZIP-it and ship-it
        ZipFile.CreateFromDirectory(tempFolder, saveArchiveFilePath);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(saveArchiveFilePath, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute | OtherRead | OtherExecute);
        Directory.Delete(tempFolder, true); //Cleanup
        
        chapter.Downloaded = true;
        DbContext.Sync();

        return [];
    }
    
    private void ProcessImage(string imagePath)
    {
        if (!TrangaSettings.bwImages && TrangaSettings.compression == 100)
        {
            Log.Debug("No processing requested for image");
            return;
        }
        
        Log.Debug($"Processing image: {imagePath}");

        try
        {
            using Image image = Image.Load(imagePath);
            if (TrangaSettings.bwImages)
                image.Mutate(i => i.ApplyProcessor(new AdaptiveThresholdProcessor()));
            File.Delete(imagePath);
            image.SaveAsJpeg(imagePath, new JpegEncoder()
            {
                Quality = TrangaSettings.compression
            });
        }
        catch (Exception e)
        {
            if (e is UnknownImageFormatException or NotSupportedException)
            {
                //If the Image-Format is not processable by ImageSharp, we can't modify it.
                Log.Debug($"Unable to process {imagePath}: Not supported image format");
            }else if (e is InvalidImageContentException)
            {
                Log.Debug($"Unable to process {imagePath}: Invalid Content");
            }
            else
            {
                Log.Error(e);
            }
        }
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
        
        //TODO MangaConnector Selection
        MangaConnectorId<Manga> mcId = manga.MangaConnectorIds.First();

        Log.Info($"Copying cover to {publicationFolder}");
        string? fileInCache = manga.CoverFileNameInCache ?? mcId.MangaConnector.SaveCoverImageToCache(mcId);
        if (fileInCache is null)
        {
            Log.Error($"File {fileInCache} does not exist");
            return;
        }
        
        string newFilePath = Path.Join(publicationFolder, $"cover.{Path.GetFileName(fileInCache).Split('.')[^1]}" );
        File.Copy(fileInCache, newFilePath, true);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(newFilePath, GroupRead | GroupWrite | UserRead | UserWrite | OtherRead | OtherWrite);
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