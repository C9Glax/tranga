using System.IO.Compression;
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
            logger.LogDebug("Chapter is already downloaded. File {file.FileId}", file.FileId);
            return;
        }

        if (chapter.DownloadLinks!.MinBy(d => d.Priority) is not { } link)
        {
            logger.LogWarning("Chapter {ChapterId} has no DownloadLinks.", ChapterId);
            return;
        }
        logger.LogDebug("Got {link.DownloadExtension} {link.Identifier}.", link.DownloadExtension, link.Identifier);
        if (DownloadExtensionsCollection.GetExtension(link.DownloadExtension) is not { } extension)
        {
            logger.LogError("Could not find {nameof(IDownloadExtension)} {link.DownloadExtension}",
                nameof(IDownloadExtension), link.DownloadExtension);
            return;
        }

        logger.LogDebug("Getting Chapter images...");
        if (await extension.GetChapterImages(link.ToChapterInfo(), stoppingToken) is not { } images)
        {
            logger.LogError("Could not get images!");
            return;
        }
        logger.LogDebug("Got {images.Count} images.", images.Count);

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
        logger.LogTrace("Built archive with {images.Count} entries.", images.Count);

        // Get Manga directory Path
        if (await _ctx.GetManga(MangaId, stoppingToken) is not { Metadata: { Series: { } seriesName } })
        {
            logger.LogError("Could not Manga (directoryName)!");
            return;
        }

        string directoryPath = Path.Join(Constants.MangaDirectory, seriesName.SafeFilesystemString());

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
        logger.LogDebug("Saved File {dbFile.FullPath}", dbFile.FullPath);
        await _ctx.AddAsync(dbFile, stoppingToken);
        link.FileId = dbFile.FileId;

        await _ctx.SaveChangesAsync(stoppingToken);

        await scope.ServiceProvider.GetRequiredService<EventPublisher>().PublishAsync(
            new ChapterDownloadedEvent(dbFile.FullPath, MangaId, seriesName, chapter.Number, chapter.Title, chapter.Volume),
            stoppingToken);
        logger.LogInformation("Downloaded Chapter {chapter.Number} of {seriesName} to {dbFile.FullPath}",
            chapter.Number, seriesName, dbFile.FullPath);
    }

    protected override void RefreshScope(IServiceScope scope)
    {
        _ctx = scope.ServiceProvider.GetRequiredService<MangaContext>();
    }

    public override string ToString() => $"{base.ToString()} - Chapter {ChapterId}";
}