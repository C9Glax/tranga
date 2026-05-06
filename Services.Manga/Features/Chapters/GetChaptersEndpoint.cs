using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Entities;

namespace Services.Manga.Features.Chapters;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetChaptersEndpoint
{
    /// <summary>
    /// Get Chapters by ID
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="ct"></param>
    /// <returns>Chapter</returns>
    /// <response code="200">Chapters</response>
    public static async Task<Ok<Chapter[]>> Handle(MangaContext mangaContext, [FromBody] Guid[] chapterIds, CancellationToken ct)
    {
        List<DbChapter> chapters = await mangaContext.Chapters.Where(c => chapterIds.Any(id => id == c.ChapterId)).ToListAsync(ct);

        Chapter[] result = chapters.Select(chapter => new Chapter
        {
            ChapterId = chapter.ChapterId,
            MangaId = chapter.MangaId,
            Title = chapter.Title,
            Volume = chapter.Volume,
            Number = chapter.Number,
            ReleaseDate = chapter.ReleaseDate
        }).ToArray();
        return TypedResults.Ok(result);
    }
}