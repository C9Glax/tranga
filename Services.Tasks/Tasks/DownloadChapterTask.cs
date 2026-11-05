using System.IO.Compression;
using System.Xml.Serialization;
using Common.Datatypes;
using Common.Helpers;
using Common.Services.Events;
using Common.Services.Events.Events;
using Common.Settings;
using Extensions;
using Extensions.Data;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;
using Services.Manga.Helpers;
using Services.Tasks.Helpers;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Tasks;

/// <summary>
/// Downloads a <see cref="DbChapter"/> using the <see cref="DbChapterDownloadLink"/> with the highest Priority.
/// </summary>
internal sealed class DownloadChapterTask(Guid mangaId, Guid chapterId)
    : RunOnceTask(DownloadChapterTask.TaskTypeIdValue), IChapterTask
{
    internal static readonly Guid TaskTypeIdValue = Guid.Parse("87d2b155-5723-4483-a2f9-c15292a14f44");

    private static readonly XmlSerializer ComicInfoSerializer =
        new(typeof(ConcreteComicInfo), new XmlRootAttribute("ComicInfo"));

    /// <summary>
    /// A <see cref="DownloadChapterTask"/> reads the <see cref="DbChapterDownloadLink"/>s a
    /// <see cref="GetMangaChaptersTask"/> creates, so it must wait for any in-flight fetch for the same Manga to
    /// finish first (see <see cref="Services.Tasks.WorkerLogic.TaskDependencyResolver"/>).
    /// </summary>
    internal override IReadOnlyCollection<Guid> DependsOnTaskTypeIds { get; } = [GetMangaChaptersTask.TaskTypeIdValue];

    public Guid MangaId { get; init; } = mangaId;

    public Guid ChapterId { get; init; } = chapterId;

    private MangaContext _ctx = null!;

    protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        logger.LogDebug("Downloading Chapter {ChapterId} of Manga {MangaId}...", ChapterId, MangaId);
        if (await _ctx.Chapters.Include(c => c.DownloadLinks)
                .SingleOrDefaultAsync(c => c.ChapterId == ChapterId, stoppingToken)
            is not { } chapter)
        {
            logger.LogError("Could not find Chapter {ChapterId}", ChapterId);
            return;
        }

        if (chapter.DownloadLinks!.FirstOrDefault(d => d.FileId != null) is { } file)
        {
            logger.LogDebug("Chapter {ChapterId} is already downloaded. File {FileId}", ChapterId, file.FileId);
            return;
        }

        if (chapter.DownloadLinks!.MinBy(d => d.Priority) is not { } link)
        {
            logger.LogWarning("Chapter {ChapterId} has no DownloadLinks.", ChapterId);
            return;
        }
        logger.LogDebug(
            "Chapter {Number} ('{Title}') has {LinkCount} DownloadLink(s); using {DownloadExtension} {Identifier} ({Url}).",
            chapter.Number, chapter.Title, chapter.DownloadLinks!.Count, link.DownloadExtension, link.Identifier, link.Url);
        if (DownloadExtensionsCollection.GetExtension(link.DownloadExtension) is not { } extension)
        {
            logger.LogError("Could not find {ExtensionType} {DownloadExtension}",
                nameof(IDownloadExtension), link.DownloadExtension);
            return;
        }

        logger.LogDebug("Getting Chapter images...");
        List<ChapterImage>? images;
        try
        {
            images = await extension.GetChapterImages(link.ToChapterInfo(), stoppingToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            logger.LogError(e, "Fetching images of Chapter {Number} via {DownloadExtension} {Identifier} threw.",
                chapter.Number, link.DownloadExtension, link.Identifier);
            throw;
        }

        if (images is null)
        {
            logger.LogError("Could not get images of Chapter {Number} via {DownloadExtension} {Identifier} ({Url})!",
                chapter.Number, link.DownloadExtension, link.Identifier, link.Url);
            return;
        }
        logger.LogDebug("Got {ImageCount} images.", images.Count);

        // Get Manga directory Path
        if (await _ctx.GetManga(MangaId, stoppingToken) is not { Metadata: { Series: { } seriesName } })
        {
            logger.LogError("Could not get Manga {MangaId} with chosen Metadata Series (needed for directory name)!", MangaId);
            return;
        }

        string directoryPath = Path.Join(Constants.MangaDirectory, seriesName.SafeFilesystemString());

        // Create archive
        using MemoryStream archiveStream = new();
        await using ZipArchive archive = new(archiveStream, ZipArchiveMode.Create, true);
        foreach (ChapterImage image in images)
        {
            ZipArchiveEntry entry = archive.CreateEntry($"{image.order}.jpg", CompressionLevel.SmallestSize);
            await using Stream entryStream = await entry.OpenAsync(stoppingToken);
            image.image.Position = 0;
            await image.image.CopyToAsync(entryStream, stoppingToken);
        }

        // Write ComicInfo.xml so readers like Komga order chapters by Volume/Number instead of falling back
        // to filename sorting, which misorders volumed and volume-less chapters relative to each other.
        ZipArchiveEntry comicInfoEntry = archive.CreateEntry("ComicInfo.xml", CompressionLevel.SmallestSize);
        await using (Stream comicInfoStream = await comicInfoEntry.OpenAsync(stoppingToken))
            ComicInfoSerializer.Serialize(comicInfoStream, chapter.CreateComicInfo(seriesName));

        logger.LogTrace("Built archive with {ImageCount} entries ({ArchiveBytes} bytes).", images.Count, archiveStream.Length);

        // Create dbFile entry for File
        DbFile dbFile = new()
        {
            Path = directoryPath,
            Name = chapter.CreateFileName(),
            MimeType = "application/zip"
        };
        // Save file
        // ReSharper disable once DisposeOnUsingVariable
        // For some reason you need to dispose the archive to write headers
        await archive.DisposeAsync();
        await dbFile.SaveFile(archiveStream, stoppingToken);
        logger.LogDebug("Saved File {FullPath}", dbFile.FullPath);
        await _ctx.AddAsync(dbFile, stoppingToken);
        link.FileId = dbFile.FileId;

        await _ctx.SaveChangesAsync(stoppingToken);

        await scope.ServiceProvider.GetRequiredService<EventPublisher>().PublishAsync(
            new ChapterDownloadedEvent(dbFile.FullPath, MangaId, seriesName, chapter.Number, chapter.Title, chapter.Volume),
            stoppingToken);
        logger.LogInformation("Downloaded Chapter {Number} of {SeriesName} to {FullPath}",
            chapter.Number, seriesName, dbFile.FullPath);
    }

    protected override void RefreshScope(IServiceScope scope)
    {
        _ctx = scope.ServiceProvider.GetRequiredService<MangaContext>();
    }

    public override string ToString() => $"{base.ToString()} - Chapter {ChapterId}";
}