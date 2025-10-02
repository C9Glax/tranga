using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Text;
using API.MangaConnectors;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using API.Workers.PeriodicWorkers;
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
    private readonly string _mangaConnectorIdId = chId.Key;
    protected override async Task<BaseWorker[]> DoWorkInternal()
    {
        Log.Debug($"Downloading chapter for MangaConnectorId {_mangaConnectorIdId}...");
        // Getting MangaConnector info
        if (await DbContext.MangaConnectorToChapter
                .Include(id => id.Obj)
                .ThenInclude(c => c.ParentManga)
                .ThenInclude(m => m.Library)
                .FirstOrDefaultAsync(c => c.Key == _mangaConnectorIdId, CancellationToken) is not { } mangaConnectorId)
        {
            Log.Error("Could not get MangaConnectorId.");
            return [];
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
            return [];
        }
        
        Log.Debug($"Downloading chapter for MangaConnectorId {mangaConnectorId}...");
        
        Chapter chapter = mangaConnectorId.Obj;
        if (chapter.ParentManga.LibraryId is null)
        {
            Log.Info($"Library is not set for {chapter.ParentManga} {chapter}");
            return [];
        }
        
        Log.Info($"Getting imageUrls for chapter {chapter}");
        string[] imageUrls = mangaConnector.GetChapterImageUrls(mangaConnectorId);
        if (imageUrls.Length < 1)
        {
            Log.Info($"No imageUrls for chapter {chapter}");
            mangaConnectorId.UseForDownload = false; // Do not try to download from this again
            if(await DbContext.Sync(CancellationToken, GetType(), "Disable Id") is { success: false } result)
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
                Directory.CreateDirectory(directoryPath, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute );
            else
                Directory.CreateDirectory(directoryPath);
        }

        Log.Info($"Downloading images: {chapter}");
        List<Stream> images = [];
        //Download all Images to temporary Folder
        foreach (string imageUrl in imageUrls)
        {
            try
            {
                if (DownloadImage(imageUrl) is not { } stream)
                {
                    Log.Error($"Failed to download image: {imageUrl}");
                    return [];
                }
                else
                    images.Add(stream);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                images.ForEach(i => i.Dispose());
            }
        }
        
        await CopyCoverFromCacheToDownloadLocation(chapter.ParentManga);
        
        Log.Debug($"Creating ComicInfo.xml {chapter}");
        foreach (CollectionEntry collectionEntry in DbContext.Entry(chapter.ParentManga).Collections)
            await collectionEntry.LoadAsync(CancellationToken);
        string comicInfo = chapter.GetComicInfoXmlString();

        if (File.Exists(saveArchiveFilePath))
        {
            Log.Info($"Archive {saveArchiveFilePath} already existed, overwriting.");
            File.Delete(saveArchiveFilePath);
        }

        //Create cbz archive
        try
        {
            Log.Debug($"Creating archive: {saveArchiveFilePath}");
            //ZIP-it and ship-it
            using ZipArchive archive = ZipFile.Open(saveArchiveFilePath, ZipArchiveMode.Create);
            Log.Debug("Writing ComicInfo.xml");
            Stream comicStream = archive.CreateEntry("ComicInfo.xml").Open();
            await comicStream.WriteAsync(Encoding.UTF8.GetBytes(comicInfo), CancellationToken);
            await comicStream.DisposeAsync();
            for (int i = 0; i < images.Count; i++)
            {
                Log.Debug($"Packaging images to archive {chapter} , image {i}");
                Stream zipStream = archive.CreateEntry($"{i}.jpg").Open();
                Stream imageStream = images[i];
                imageStream.Position = 0;
                await imageStream.CopyToAsync(zipStream, CancellationToken);
                await zipStream.DisposeAsync();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex);
        }
        finally
        {
            images.ForEach(i => i.Dispose());
        }
        
        Log.Debug("Setting Permissions");
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(saveArchiveFilePath, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute );

        DbContext.Entry(chapter).Property(c => c.Downloaded).CurrentValue = true;
        if(await DbContext.Sync(CancellationToken, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } e)
            Log.Error($"Failed to save database changes: {e.exceptionMessage}");
        
        Log.Debug($"Downloaded chapter {chapter}.");

        bool refreshLibrary = await CheckLibraryRefresh();
        if(refreshLibrary)
            Log.Info($"Condition {Tranga.Settings.LibraryRefreshSetting} met.");

        return refreshLibrary? [new RefreshLibrariesWorker()] : [];
    }

    private async Task<bool> CheckLibraryRefresh() => Tranga.Settings.LibraryRefreshSetting switch
    {
        LibraryRefreshSetting.AfterAllFinished => await AllDownloadsFinished(),
        LibraryRefreshSetting.AfterMangaFinished => await DbContext.MangaConnectorToChapter.Include(chId => chId.Obj).Where(chId => chId.UseForDownload).AllAsync(chId => chId.Obj.Downloaded, CancellationToken),
        LibraryRefreshSetting.AfterEveryChapter => true,
        LibraryRefreshSetting.WhileDownloading => await AllDownloadsFinished() ||  DateTime.UtcNow.Subtract(RefreshLibrariesWorker.LastRefresh).TotalMinutes > Tranga.Settings.RefreshLibraryWhileDownloadingEveryMinutes,
        _ => true
    };
    private async Task<bool> AllDownloadsFinished() => (await StartNewChapterDownloadsWorker.GetMissingChapters(DbContext, CancellationToken)).Count == 0;
    
    private bool ProcessImage(Stream imageStream, out Stream processedImage)
    {
        Log.Debug("Processing image");
        if (!Tranga.Settings.BlackWhiteImages && Tranga.Settings.ImageCompression == 100)
        {
            Log.Debug("No processing requested for image");
            processedImage = imageStream;
            return true;
        }

        processedImage = new MemoryStream();
        try
        {
            using Image image = Image.Load(imageStream);
            Log.Debug("Image loaded");
            if (Tranga.Settings.BlackWhiteImages)
                image.Mutate(i => i.ApplyProcessor(new AdaptiveThresholdProcessor()));
            image.SaveAsJpeg(processedImage, new JpegEncoder()
            {
                Quality = Tranga.Settings.ImageCompression
            });
            Log.Debug("Image processed");
            return true;
        }
        catch (Exception e)
        {
            if (e is UnknownImageFormatException or NotSupportedException)
            {
                //If the Image-Format is not processable by ImageSharp, we can't modify it.
                Log.Debug("Unable to process image: Not supported image format");
            }else if (e is InvalidImageContentException)
            {
                Log.Debug("Unable to process image: Invalid Content");
            }
            else
            {
                Log.Error(e);
            }
            return false;
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

    private Stream? DownloadImage(string imageUrl)
    {
        HttpDownloadClient downloadClient = new();
        RequestResult requestResult = downloadClient.MakeRequest(imageUrl, RequestType.MangaImage);

        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return null;
        if (requestResult.result == Stream.Null)
            return null;
        
        return ProcessImage(requestResult.result, out Stream processedImage) ? processedImage : null;
    }

    public override string ToString() => $"{base.ToString()} {_mangaConnectorIdId}";
}