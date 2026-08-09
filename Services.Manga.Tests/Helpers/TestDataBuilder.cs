using Services.Manga.Database;

namespace Services.Manga.Tests.Helpers;

/// <summary>
/// Builds and persists minimal, valid database graphs shared across endpoint tests.
/// </summary>
public static class TestDataBuilder
{
    public static DbMetadata NewMetadata(string series = "Series", Guid? metadataExtension = null, Guid? coverId = null) => new()
    {
        MetadataId = Guid.NewGuid(),
        MetadataExtension = metadataExtension ?? Guid.NewGuid(),
        Identifier = Guid.NewGuid().ToString(),
        Series = series,
        Summary = "Summary",
        CoverId = coverId
    };

    public static DbManga NewManga(bool monitored = true) => new()
    {
        MangaId = Guid.NewGuid(),
        Monitored = monitored
    };

    public static DbDownloadLink NewDownloadLink(string series = "Series", Guid? downloadExtension = null, Guid? coverId = null) => new()
    {
        DownloadLinkId = Guid.NewGuid(),
        DownloadExtension = downloadExtension ?? Guid.NewGuid(),
        Identifier = Guid.NewGuid().ToString(),
        Series = series,
        Summary = "Summary",
        Url = "https://example.com",
        CoverId = coverId
    };

    /// <summary>
    /// Persists a Manga with one Metadata-Entry marked as chosen "Source of Truth" -
    /// the shape most manga-scoped read endpoints expect.
    /// </summary>
    public static async Task<(DbManga Manga, DbMetadata Metadata, DbMangaMetadataEntries Entry)> SeedMangaWithChosenMetadata(
        MangaContext context, bool monitored = true, string series = "Series", Guid? coverId = null, CancellationToken ct = default)
    {
        DbManga manga = NewManga(monitored);
        DbMetadata metadata = NewMetadata(series, coverId: coverId);
        DbMangaMetadataEntries entry = new()
        {
            MangaId = manga.MangaId,
            MetadataId = metadata.MetadataId,
            Chosen = true,
            Manga = manga,
            Metadata = metadata
        };

        await context.AddAsync(manga, ct);
        await context.AddAsync(metadata, ct);
        await context.AddAsync(entry, ct);
        await context.SaveChangesAsync(ct);

        return (manga, metadata, entry);
    }

    public static async Task<DbMangaDownloadLinks> SeedMangaDownloadLink(
        MangaContext context, DbManga manga, bool matched = true, int priority = 0, CancellationToken ct = default)
    {
        DbDownloadLink downloadLink = NewDownloadLink();
        DbMangaDownloadLinks link = new()
        {
            MangaId = manga.MangaId,
            DownloadLinkId = downloadLink.DownloadLinkId,
            Matched = matched,
            Priority = priority,
            Manga = manga,
            DownloadLink = downloadLink
        };

        await context.AddAsync(downloadLink, ct);
        await context.AddAsync(link, ct);
        await context.SaveChangesAsync(ct);

        return link;
    }

    public static async Task<DbChapter> SeedChapter(
        MangaContext context, DbManga manga, string number = "1", CancellationToken ct = default)
    {
        DbChapter chapter = new()
        {
            ChapterId = Guid.NewGuid(),
            MangaId = manga.MangaId,
            Number = number
        };

        await context.AddAsync(chapter, ct);
        await context.SaveChangesAsync(ct);

        return chapter;
    }

    public static async Task<DbChapterDownloadLink> SeedChapterDownloadLink(
        MangaContext context, DbChapter chapter, bool downloaded, CancellationToken ct = default)
    {
        Guid? fileId = null;
        if (downloaded)
        {
            DbFile file = new() { FileId = Guid.NewGuid(), Path = "/downloads", Name = "chapter.cbz", MimeType = "application/zip" };
            await context.AddAsync(file, ct);
            fileId = file.FileId;
        }

        DbChapterDownloadLink link = new()
        {
            ChapterId = chapter.ChapterId,
            DownloadExtension = Guid.NewGuid(),
            Identifier = Guid.NewGuid().ToString(),
            Priority = 0,
            FileId = fileId,
            Chapter = chapter
        };

        await context.AddAsync(link, ct);
        await context.SaveChangesAsync(ct);

        return link;
    }
}
