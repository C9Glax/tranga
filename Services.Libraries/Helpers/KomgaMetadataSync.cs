using Common.Helpers;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;

namespace Services.Libraries.Helpers;

/// <summary>
/// Pushes Tranga's chosen metadata (title, summary, cover) for a manga to a linked Komga series.
/// Shared by every place that needs to sync metadata after a manga gets linked to a Komga series
/// (initial connect backfill, first-chapter mapping creation, and manual metadata-source changes).
/// </summary>
internal static class KomgaMetadataSync
{
    public static async Task PushMetadata(MangaContext mangaContext, Extensions.Extensions.Komga komga, string seriesId, Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.GetManga(mangaId, ct) is not { } mangaMetadataEntry)
            return;

        DbMetadata metadata = mangaMetadataEntry.Metadata;

        await komga.UpdateSeriesMetadata(new Extensions.Extensions.KomgaSeries(seriesId, metadata.Series, metadata.Summary ?? ""), ct);

        if (metadata.CoverId is { } coverId)
        {
            if (await mangaContext.Files.FirstOrDefaultAsync(f => f.FileId == coverId, ct) is { } file)
            {
                MemoryStream fileBytes = await file.LoadFile(ct);
                TrangaImage image = new();
                await fileBytes.CopyToAsync(image, ct);
                image.Position = 0;
                await komga.UpdateSeriesPoster(seriesId, image, ct);
            }
        }
    }
}
