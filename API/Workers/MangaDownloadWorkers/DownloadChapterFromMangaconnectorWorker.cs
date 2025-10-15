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

        if (chapter.FullArchiveFilePath is not { } saveArchiveFilePath)
        {
            Log.Error("Failed getting saveArchiveFilePath");
            return [];
        }
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
                if (await mangaConnector.DownloadImage(imageUrl, CancellationToken) is not { } stream)
                {
                    Log.Error($"Failed to download image: {imageUrl}");
                    return [];
                }
                else
                    images.Add(await ProcessImage(stream, CancellationToken));
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                images.ForEach(i => i.Dispose());
                return [];
            }
        }
        
        await CopyCoverFromCacheToDownloadLocation(chapter.ParentManga);
        
        Log.Debug($"Loading collections {chapter}");
        foreach (CollectionEntry collectionEntry in DbContext.Entry(chapter.ParentManga).Collections)
            await collectionEntry.LoadAsync(CancellationToken);

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

            if (Constants.CreateComicInfoXml)
            {
                Log.Debug("Writing ComicInfo.xml");
                Stream comicStream = archive.CreateEntry("ComicInfo.xml").Open();
                string comicInfo = chapter.GetComicInfoXmlString();
                await comicStream.WriteAsync(Encoding.UTF8.GetBytes(comicInfo), CancellationToken);
                await comicStream.DisposeAsync();
            }
            else
                Log.Debug("Skipping ComicInfo.xml. CREATE_COMICINFO_XML is set to false");
            
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

        chapter.Downloaded = true;
        chapter.FileName = new FileInfo(saveArchiveFilePath).Name;
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
    
    private async Task<Stream> ProcessImage(Stream imageStream, CancellationToken? cancellationToken = null)
    {
        Log.Debug("Processing image");
        imageStream.Position = 0;
        if (!Tranga.Settings.BlackWhiteImages && Tranga.Settings.ImageCompression == 100)
        {
            Log.Debug("No processing requested for image");
            return imageStream;
        }

        MemoryStream processedImage = new ();
        try
        {
            using Image image = await Image.LoadAsync(imageStream, cancellationToken ?? CancellationToken.None);
            Log.Debug("Image loaded");
            if (Tranga.Settings.BlackWhiteImages)
                image.Mutate(i => i.ApplyProcessor(new AdaptiveThresholdProcessor()));
            await image.SaveAsJpegAsync(processedImage, new JpegEncoder()
            {
                Quality = Tranga.Settings.ImageCompression
            });
            Log.Debug("Image processed");
            processedImage.Position = 0;
            return processedImage;
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
            await imageStream.CopyToAsync(processedImage);
            processedImage.Position = 0;
            return processedImage;
        }
    }
    
    private async Task CopyCoverFromCacheToDownloadLocation(Manga manga)
    {
        Log.Debug($"Copying cover for {manga}");

        manga = await DbContext.MangaIncludeAll().FirstAsync(m => m.Key == manga.Key, CancellationToken);
        string publicationFolder;
        try
        {
            Log.Debug("Checking Manga directory exists...");
            //Check if Publication already has a Folder and cover
            publicationFolder = manga.FullDirectoryPath;

            Log.Debug("Checking cover already exists...");
            DirectoryInfo dirInfo = new(publicationFolder);
            if (dirInfo.EnumerateFiles()
                .Any(info => info.Name.Contains("cover", StringComparison.InvariantCultureIgnoreCase)))
            {
                Log.Debug($"Cover already exists at {publicationFolder}");
                return;
            }
        }
        catch (Exception e)
        {
            Log.Error(e);
            return;
        }

        if (manga.CoverFileNameInCache is not { } coverFileNameInCache)
        {
            MangaConnectorId<Manga> mangaConnectorId = manga.MangaConnectorIds.First();
            if (!Tranga.TryGetMangaConnector(mangaConnectorId.MangaConnectorName, out MangaConnector? mangaConnector))
            {
                Log.Error($"MangaConnector with name {mangaConnectorId.MangaConnectorName} could not be found");
                return;
            }
            
            coverFileNameInCache = mangaConnector.SaveCoverImageToCache(mangaConnectorId);
            manga.CoverFileNameInCache = coverFileNameInCache;
            if (await DbContext.Sync(CancellationToken, reason: "Update cover filename") is { success: false } result)
                Log.Error($"Couldn't update cover filename {result.exceptionMessage}");
        }
        if (coverFileNameInCache is null)
        {
            Log.Error($"File {coverFileNameInCache} does not exist and failed to download cover");
            return;
        }
        
        string fullCoverPath = Path.Join(TrangaSettings.CoverImageCacheOriginal, coverFileNameInCache);
        string newFilePath = Path.Join(publicationFolder, $"cover.{Path.GetFileName(coverFileNameInCache).Split('.')[^1]}" );
        File.Copy(fullCoverPath, newFilePath, true);
        if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(newFilePath, GroupRead | GroupWrite | UserRead | UserWrite | OtherRead | OtherWrite);
        Log.Debug($"Copied cover from {fullCoverPath} to {newFilePath}");
    }

    public override string ToString() => $"{base.ToString()} {_mangaConnectorIdId}";
}