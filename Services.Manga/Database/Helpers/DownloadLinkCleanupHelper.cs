using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Services.Manga.Database.Helpers;

/// <summary>
/// Removes the download links that point at download extensions which no longer exist.
/// </summary>
public static class DownloadLinkCleanupHelper
{
    /// <summary>
    /// Deletes every download link that refers to one of <paramref name="removedExtensionIds"/>: the manga's
    /// <see cref="DbMangaDownloadLinks"/> matches, the <see cref="DbDownloadLink"/> candidates themselves and the
    /// <see cref="DbChapterDownloadLink"/>s that were never downloaded.
    /// <para>
    /// Chapter links that already hold a downloaded file are kept: they are what marks the chapter as downloaded, and
    /// nothing reads them again once the file exists. Mangas, chapters, metadata and files are left untouched.
    /// </para>
    /// </summary>
    /// <param name="mangaContext">The database context to operate on.</param>
    /// <param name="removedExtensionIds">The identifiers of the download extensions that were removed.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The number of <see cref="DbDownloadLink"/> candidates deleted.</returns>
    public static async Task<int> DeleteLinksOfRemovedExtensions(this MangaContext mangaContext,
        ICollection<Guid> removedExtensionIds, CancellationToken ct)
    {
        if (removedExtensionIds.Count == 0)
            return 0;

        await using IDbContextTransaction transaction = await mangaContext.Database.BeginTransactionAsync(ct);

        await mangaContext.MangaDownloadLinks
            .Where(l => removedExtensionIds.Contains(l.DownloadLink.DownloadExtension))
            .ExecuteDeleteAsync(ct);

        int deletedLinks = await mangaContext.DownloadLinks
            .Where(l => removedExtensionIds.Contains(l.DownloadExtension))
            .ExecuteDeleteAsync(ct);

        await mangaContext.ChapterDownloadLinks
            .Where(l => removedExtensionIds.Contains(l.DownloadExtension) && l.FileId == null)
            .ExecuteDeleteAsync(ct);

        await transaction.CommitAsync(ct);
        return deletedLinks;
    }
}
