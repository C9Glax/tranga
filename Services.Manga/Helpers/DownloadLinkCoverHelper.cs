using Common.Helpers;
using Common.Settings;
using Extensions;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;

namespace Services.Manga.Helpers;

/// <summary>
/// Gives a <see cref="DbDownloadLink"/> its cover image: stored as a <see cref="DbFile"/> in the cover directory, the
/// same way for links found by a search and for links added by URL.
/// </summary>
internal static class DownloadLinkCoverHelper
{
    /// <summary>
    /// Stores <paramref name="cover"/> (converted to JPEG) as the cover of <paramref name="downloadLink"/>.
    /// The changes are not saved to the database.
    /// </summary>
    /// <returns><see langword="false"/> if the data is not a valid or supported image (nothing is stored then).</returns>
    public static async Task<bool> SaveCover(this MangaContext mangaContext, DbDownloadLink downloadLink, TrangaImage cover, CancellationToken ct)
    {
        try
        {
            await cover.ToJpeg(ct);
        }
        catch
        {
            // Cover data isn't a valid/supported image - skip saving it rather than storing the raw,
            // undecoded bytes under a made-up MIME type (consumers like Komga reject/fail to decode those).
            return false;
        }

        await mangaContext.StoreCover(downloadLink, cover, "image/jpeg", ".jpg", ct);
        return true;
    }

    /// <summary>
    /// Gives <paramref name="downloadLink"/> a copy of the cover file with the id <paramref name="sourceCoverId"/> (the
    /// manga's own cover). It has to be a copy: a file belongs to exactly one metadata or download link (one-to-one,
    /// deleted along with it). The changes are not saved to the database.
    /// </summary>
    /// <returns><see langword="false"/> if there is no such cover or it cannot be read.</returns>
    public static async Task<bool> CopyCover(this MangaContext mangaContext, DbDownloadLink downloadLink, Guid? sourceCoverId, CancellationToken ct)
    {
        if (sourceCoverId is not { } coverId || await mangaContext.Files.FirstOrDefaultAsync(f => f.FileId == coverId, ct) is not { } source)
            return false;

        TrangaImage copy = new();
        try
        {
            await using MemoryStream data = await source.LoadFile(ct);
            await data.CopyToAsync(copy, ct);
        }
        catch (FileLoadException)
        {
            return false;
        }

        // The copy is not converted again: the original is already a stored cover.
        await mangaContext.StoreCover(downloadLink, copy, source.MimeType, Path.GetExtension(source.Name), ct);
        return true;
    }

    /// <summary>
    /// Gives a link that was added by URL (and so has no search result with a cover) its cover: the one of its series
    /// on the extension's site, or if the extension cannot provide one, a copy of the manga's own cover
    /// (<paramref name="mangaCoverId"/>). Never throws because of the cover; the link just stays without one.
    /// The changes are not saved to the database.
    /// </summary>
    public static async Task AddCover(this MangaContext mangaContext, DbDownloadLink downloadLink, IDownloadExtension extension,
        Guid? mangaCoverId, CancellationToken ct)
    {
        try
        {
            if (await extension.GetCover(downloadLink.ToMangaInfo(), ct) is { } cover &&
                await mangaContext.SaveCover(downloadLink, cover, ct))
                return;
        }
        catch (Exception) when (!ct.IsCancellationRequested)
        {
            // Not being able to get the cover is no reason to refuse the link: use the fallback below.
        }

        await mangaContext.CopyCover(downloadLink, mangaCoverId, ct);
    }

    private static async Task StoreCover(this MangaContext mangaContext, DbDownloadLink downloadLink, MemoryStream data,
        string mimeType, string extension, CancellationToken ct)
    {
        Guid coverId = Guid.CreateVersion7();
        DbFile file = new()
        {
            FileId = coverId,
            Name = $"{coverId}{extension}",
            Path = Constants.CoverDirectory,
            MimeType = mimeType
        };
        await mangaContext.AddAsync(file, ct);
        await file.SaveFile(data, ct);
        downloadLink.CoverId = file.FileId;
        downloadLink.Cover = file;
    }
}
