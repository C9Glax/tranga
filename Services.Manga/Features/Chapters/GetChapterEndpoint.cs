using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Entities;

namespace Services.Manga.Features.Chapters;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetChapterEndpoint
{
    /// <summary>
    /// Get Chapter by ID
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="chapterId">ID of Chapter</param>
    /// <param name="ct"></param>
    /// <returns>Chapter</returns>
    /// <response code="200">Chapter</response>
    /// <response code="404">Chapter with requested ID does not exist</response>
    public static async Task<Results<Ok<Chapter>, NotFound>> Handle(MangaContext mangaContext, [FromRoute] Guid chapterId, CancellationToken ct)
    {
        if (await mangaContext.Chapters.SingleOrDefaultAsync(c => c.ChapterId == chapterId, ct) is not { } chapter)
            return TypedResults.NotFound();

        return TypedResults.Ok(new Chapter
        {
            ChapterId = chapter.ChapterId,
            MangaId = chapter.MangaId,
            Title = chapter.Title,
            Volume = chapter.Volume,
            Number = chapter.Number,
            ReleaseDate = chapter.ReleaseDate
        });
    }
}