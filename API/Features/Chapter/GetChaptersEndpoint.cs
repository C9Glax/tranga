using API.DTOs;
using API.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Chapter;

/// <summary>
/// Get the Chapters of a DownloadLink
/// </summary>
public abstract class GetChaptersEndpoint
{
    /// <summary>
    /// Get the Chapters of a DownloadLink
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="matchId">ID of the DownloadLink</param>
    /// <param name="ct"></param>
    /// <response code="200">Chapters</response>
    /// <response code="404">Chapters could not be found</response>
    public static async Task<Results<Ok<ChapterDTO[]>, NotFound>> Handle(MangaContext mangaContext, [FromRoute] Guid matchId, CancellationToken ct)
    {
        if (await mangaContext.Chapters.Where(c => c.DownloadLinkId == matchId).ToListAsync(ct) is not { } chapters)
            return TypedResults.NotFound();

        return TypedResults.Ok(chapters.Select(c => c.ToDTO()).ToArray());
    }
    
}