using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Entities;
using Services.Manga.Helpers;

namespace Services.Manga.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetMangaChaptersEndpoint
{
    /// <summary>
    /// Chapters of Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of Manga</param>
    /// <param name="ct"></param>
    /// <returns>List of Chapters of Manga</returns>
    /// <response code="200">List of Chapters of Manga</response>
    public static async Task<Ok<MangaChapter[]>> Handle(MangaContext mangaContext, [FromRoute] Guid mangaId, CancellationToken ct)
    {
        List<DbChapter> chapters = await mangaContext.Chapters
            .Include(c => c.DownloadLinks)
            .Where(c => c.MangaId == mangaId)
            .ToListAsync(ct);

        MangaChapter[] result = chapters.Select(c => c.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}
