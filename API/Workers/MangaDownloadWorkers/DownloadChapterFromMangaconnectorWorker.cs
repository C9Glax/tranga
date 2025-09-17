using System.IO.Compression;
using System.Runtime.InteropServices;
using API.MangaConnectors;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Binarization;
using static System.IO.UnixFileMode;

namespace API.Workers.MangaDownloadWorkers;

/// <summary>
/// Downloads single chapter for Manga from Mangaconnector
/// </summary>
/// <param name="chId"></param>
/// <param name="dependsOn"></param>
public class DownloadChapterFromMangaconnectorWorker(MangaConnectorId<Chapter> chId, IEnumerable<BaseWorker>? dependsOn = null)
    : BaseWorkerWithContext<MangaContext>(dependsOn)
{
    internal readonly string MangaConnectorIdId = chId.Key;
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug($"Downloading chapter for MangaConnectorId {MangaConnectorIdId}...");
        // Getting MangaConnector info
        if (await DbContext.MangaConnectorToChapter
                .Include(id => id.Obj)
                .ThenInclude(c => c.ParentManga)
                .ThenInclude(m => m.Library)
                .FirstOrDefaultAsync(c => c.Key == MangaConnectorIdId, CancellationToken) is not { } mangaConnectorId)
        {
            Log.Error("Could not get MangaConnectorId.");
            return []; //TODO Exception?
        }
        
        // Check if Chapter already exists...
        if (await mangaConnectorId.Obj.CheckDownloaded(DbContext, CancellationToken))
        {
            Log.Warn("Chapter already exists!");
            return [];
        }
        
        if (!Tranga.TryGetMangaConnector(mangaConnectorId.MangaConnectorName, out MangaConnector? mangaConnector))
        {
            Log.Error("Could not get MangaConnector.");
            return []; //TODO Exception?
        }
        Log.Debug($"Downloading chapter for MangaConnectorId {mangaConnectorId}...");
        
        Chapter chapter = mangaConnectorId.Obj;
        if (chapter.Downloaded)
        {
            Log.Info("Chapter was already downloaded.");
            return [];
        }
        if (chapter.ParentManga.LibraryId is null)
        {
            Log.Info($"Library is not set for {chapter.ParentManga} {chapter}");
            return [];
        }
        
        string[] imageUrls = mangaConnector.GetChapterImageUrls(mangaConnectorId);
        if (imageUrls.Length < 1)
        {
            Log.Info($"No imageUrls for chapter {chapter}");
            mangaConnectorId.UseForDownload = false; // Do not try to download from this again
            if(await DbContext.Sync(CancellationToken) is { success: false } result)
                Log.Error(result.exceptionMessage);
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
        
        await CopyCoverFromCacheToDownloadLocation(chapter.ParentManga);
        
        Log.Debug($"Creating ComicInfo.xml {chapter}");
        foreach (CollectionEntry collectionEntry in DbContext.Entry(chapter.ParentManga).Collections)
            await collectionEntry.LoadAsync(CancellationToken);
        await File.WriteAllTextAsync(Path.Join(tempFolder, "ComicInfo.xml"), chapter.GetComicInfoXmlString(), CancellationToken);
        
        Log.Debug($"Packaging images to archive {chapter}");
        //ZIP-it and ship-it
        ZipFile.CreateFromDirectory(tempFolder, saveArchiveFilePath);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(saveArchiveFilePath, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute | OtherRead | OtherExecute);
        Directory.Delete(tempFolder, true); //Cleanup

        DbContext.Entry(chapter).Property(c => c.Downloaded).CurrentValue = true;
        if(await DbContext.Sync(CancellationToken) is { success: false } e)
            Log.Error($"Failed to save database changes: {e.exceptionMessage}");
        
        Log.Debug($"Downloaded chapter {chapter}.");

        return [];
    }
    
    private void ProcessImage(string imagePath)
    {
        if (!Tranga.Settings.BlackWhiteImages && Tranga.Settings.ImageCompression == 100)
        {
            Log.Debug("No processing requested for image");
            return;
        }
        
        Log.Debug($"Processing image: {imagePath}");

        try
        {
            using Image image = Image.Load(imagePath);
            if (Tranga.Settings.BlackWhiteImages)
                image.Mutate(i => i.ApplyProcessor(new AdaptiveThresholdProcessor()));
            File.Delete(imagePath);
            image.SaveAsJpeg(imagePath, new JpegEncoder()
            {
                Quality = Tranga.Settings.ImageCompression
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
    
    private async Task CopyCoverFromCacheToDownloadLocation(Manga manga)
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
        await DbContext.Entry(manga).Collection(m => m.MangaConnectorIds).LoadAsync(CancellationToken);
        MangaConnectorId<Manga> mangaConnectorId = manga.MangaConnectorIds.First();
        if (!Tranga.TryGetMangaConnector(mangaConnectorId.MangaConnectorName, out MangaConnector? mangaConnector))
        {
            Log.Error($"MangaConnector with name {mangaConnectorId.MangaConnectorName} could not be found");
            return;
        }

        Log.Info($"Copying cover to {publicationFolder}");
        await DbContext.Entry(mangaConnectorId).Navigation(nameof(MangaConnectorId<Manga>.Obj)).LoadAsync(CancellationToken);
        string? coverFileNameInCache = manga.CoverFileNameInCache ?? mangaConnector.SaveCoverImageToCache(mangaConnectorId);
        if (coverFileNameInCache is null)
        {
            Log.Error($"File {coverFileNameInCache} does not exist");
            return;
        }
        
        string fullCoverPath = Path.Join(TrangaSettings.CoverImageCacheOriginal, coverFileNameInCache);
        string newFilePath = Path.Join(publicationFolder, $"cover.{Path.GetFileName(coverFileNameInCache).Split('.')[^1]}" );
        File.Copy(fullCoverPath, newFilePath, true);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(newFilePath, GroupRead | GroupWrite | UserRead | UserWrite | OtherRead | OtherWrite);
        Log.Debug($"Copied cover from {fullCoverPath} to {newFilePath}");
    }

    private bool DownloadImage(string imageUrl, string savePath)
    {
        HttpDownloadClient downloadClient = new();
        RequestResult requestResult = downloadClient.MakeRequest(imageUrl, RequestType.MangaImage);

        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return false;
        if (requestResult.result == Stream.Null)
            return false;

        FileStream fs = new(savePath, FileMode.Create, FileAccess.Write, FileShare.None);
        requestResult.result.CopyTo(fs);
        fs.Close();
        ProcessImage(savePath);
        return true;
    }

    public override string ToString() => $"{base.ToString()} {MangaConnectorIdId}";
}