using System.IO.Compression;
using System.Xml.Serialization;
using Common.Datatypes;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;
using Services.Tasks.Helpers;
using Services.Tasks.TaskTypes;

namespace Services.Tasks.Tasks;

/// <summary>
/// One-shot task that adds a <c>ComicInfo.xml</c> entry to every already-downloaded Chapter archive that is
/// missing one - i.e. Chapters downloaded before Tranga started writing this metadata (see
/// <see cref="DownloadChapterTask"/>). Readers such as Komga rely on <c>ComicInfo.xml</c>'s Volume/Number to
/// order Chapters; without it they fall back to sorting the archive filename, which misorders volumed and
/// volume-less Chapters relative to each other.
/// </summary>
internal sealed class BackfillComicInfoTask() : RunOnceTask(BackfillComicInfoTask.TaskTypeIdValue)
{
    internal static readonly Guid TaskTypeIdValue = Guid.Parse("1345fd57-e8c2-4084-a281-157ab938bab6");

    private static readonly XmlSerializer ComicInfoSerializer =
        new(typeof(ConcreteComicInfo), new XmlRootAttribute("ComicInfo"));

    private MangaContext _ctx = null!;

    protected override async Task RunAsync(IServiceScope scope, ILogger logger, CancellationToken stoppingToken)
    {
        List<DbChapter> chaptersWithFiles = await _ctx.Chapters.Include(c => c.DownloadLinks)
            .Where(c => c.DownloadLinks!.Any(d => d.FileId != null))
            .ToListAsync(stoppingToken);
        logger.LogDebug("Checking {chaptersWithFiles.Count} downloaded Chapters for missing ComicInfo.xml...",
            chaptersWithFiles.Count);

        Dictionary<Guid, string?> seriesNameByManga = new();
        int patched = 0, skipped = 0;
        foreach (DbChapter chapter in chaptersWithFiles)
        {
            if (!seriesNameByManga.TryGetValue(chapter.MangaId, out string? seriesName))
            {
                seriesName = await _ctx.GetManga(chapter.MangaId, stoppingToken) is { Metadata.Series: { } series }
                    ? series
                    : null;
                seriesNameByManga[chapter.MangaId] = seriesName;
            }

            if (seriesName is null)
            {
                logger.LogWarning("Could not resolve series name for Manga {chapter.MangaId}, skipping Chapter {chapter.ChapterId}.",
                    chapter.MangaId, chapter.ChapterId);
                skipped++;
                continue;
            }

            Guid fileId = chapter.DownloadLinks!.First(d => d.FileId != null).FileId!.Value;
            if (await _ctx.Files.FirstOrDefaultAsync(f => f.FileId == fileId, stoppingToken) is not { } dbFile)
            {
                skipped++;
                continue;
            }

            if (await PatchComicInfo(dbFile, chapter, seriesName, logger, stoppingToken))
                patched++;
            else
                skipped++;
        }

        logger.LogInformation(
            "Added ComicInfo.xml to {patched} previously-downloaded Chapter archives ({skipped} already had one or could not be patched).",
            patched, skipped);
    }

    /// <returns><see langword="true"/> if the archive was rewritten with a new <c>ComicInfo.xml</c> entry.</returns>
    private static async Task<bool> PatchComicInfo(DbFile dbFile, DbChapter chapter, string seriesName, ILogger logger, CancellationToken ct)
    {
        MemoryStream content;
        try
        {
            content = await dbFile.LoadFile(ct);
        }
        catch (FileLoadException)
        {
            logger.LogWarning("Could not load File {dbFile.FullPath} for Chapter {chapter.ChapterId}, skipping.",
                dbFile.FullPath, chapter.ChapterId);
            return false;
        }

        using (content)
        {
            await using ZipArchive archive = new(content, ZipArchiveMode.Update, true);
            if (archive.GetEntry("ComicInfo.xml") is not null)
                return false;

            ZipArchiveEntry comicInfoEntry = archive.CreateEntry("ComicInfo.xml", CompressionLevel.SmallestSize);
            await using (Stream comicInfoStream = await comicInfoEntry.OpenAsync(ct))
                ComicInfoSerializer.Serialize(comicInfoStream, chapter.CreateComicInfo(seriesName));

            // ReSharper disable once DisposeOnUsingVariable
            // For some reason you need to dispose the archive to write headers
            await archive.DisposeAsync();
            await dbFile.SaveFile(content, ct);
        }

        return true;
    }

    protected override void RefreshScope(IServiceScope scope)
    {
        _ctx = scope.ServiceProvider.GetRequiredService<MangaContext>();
    }
}
