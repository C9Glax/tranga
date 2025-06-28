using System.ComponentModel.DataAnnotations;
using System.IO.Compression;
using System.Runtime.InteropServices;
using API.MangaDownloadClients;
using API.Schema.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Binarization;
using static System.IO.UnixFileMode;

namespace API.Schema.Jobs;

public class DownloadSingleChapterJob : Job
{
    [StringLength(64)] [Required] public string ChapterId { get; init; }

    private Chapter _chapter = null!;
    
    [JsonIgnore]
    public Chapter Chapter 
    {
        get => LazyLoader.Load(this, ref _chapter);
        init => _chapter = value;
    }

    public DownloadSingleChapterJob(Chapter chapter, Job? parentJob = null, ICollection<Job>? dependsOnJobs = null)
        : base(TokenGen.CreateToken(typeof(DownloadSingleChapterJob)), JobType.DownloadSingleChapterJob, 0, parentJob, dependsOnJobs)
    {
        this.ChapterId = chapter.ChapterId;
        this.Chapter = chapter;
    }
    
    /// <summary>
    /// EF ONLY!!!
    /// </summary>
    internal DownloadSingleChapterJob(ILazyLoader lazyLoader, string jobId, ulong recurrenceMs, string chapterId, string? parentJobId)
        : base(lazyLoader, jobId, JobType.DownloadSingleChapterJob, recurrenceMs, parentJobId)
    {
        this.ChapterId = chapterId;
    }
    
    protected override IEnumerable<Job> RunInternal(PgsqlContext context)
    {
        if (Chapter.Downloaded)
        {
            Log.Info("Chapter was already downloaded.");
            return [];
        }
        string[] imageUrls = Chapter.ParentManga.MangaConnector.GetChapterImageUrls(Chapter);
        if (imageUrls.Length < 1)
        {
            Log.Info($"No imageUrls for chapter {ChapterId}");
            return [];
        }
        context.Entry(Chapter.ParentManga).Reference<LocalLibrary>(m => m.Library).Load(); //Need to explicitly load, because we are not accessing navigation directly...
        string saveArchiveFilePath = Chapter.FullArchiveFilePath;
        Log.Debug($"Chapter path: {saveArchiveFilePath}");
        
        //Check if Publication Directory already exists
        string? directoryPath = Path.GetDirectoryName(saveArchiveFilePath);
        if (directoryPath is null)
        {
            Log.Error($"Directory path could not be found: {saveArchiveFilePath}");
            this.state = JobState.Failed;
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
        
        CopyCoverFromCacheToDownloadLocation(Chapter.ParentManga);
        
        Log.Debug($"Creating ComicInfo.xml {ChapterId}");
        File.WriteAllText(Path.Join(tempFolder, "ComicInfo.xml"), Chapter.GetComicInfoXmlString());
        
        Log.Debug($"Packaging images to archive {ChapterId}");
        //ZIP-it and ship-it
        ZipFile.CreateFromDirectory(tempFolder, saveArchiveFilePath);
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            File.SetUnixFileMode(saveArchiveFilePath, UserRead | UserWrite | UserExecute | GroupRead | GroupWrite | GroupExecute | OtherRead | OtherExecute);
        Directory.Delete(tempFolder, true); //Cleanup
        
        Chapter.Downloaded = true;
        context.SaveChanges();

        if (context.Jobs.ToList().Any(j =>
            {
                if (j.JobType != JobType.UpdateChaptersDownloadedJob)
                    return false;
                UpdateChaptersDownloadedJob job = (UpdateChaptersDownloadedJob)j;
                return job.MangaId == this.Chapter.ParentMangaId;
            }))
            return [];

        return [new UpdateChaptersDownloadedJob(Chapter.ParentManga, 0, this.ParentJob)];
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
            Log.Error(e);
            if (e is UnknownImageFormatException or NotSupportedException)
            {
                //If the Image-Format is not processable by ImageSharp, we can't modify it.
                Log.Debug($"Unable to process {imagePath}: Not supported image format");
            }else if (e is InvalidImageContentException)
            {
                Log.Debug($"Unable to process {imagePath}: Invalid Content");
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

        Log.Info($"Copying cover to {publicationFolder}");
        string? fileInCache = manga.CoverFileNameInCache ?? manga.MangaConnector.SaveCoverImageToCache(manga);
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