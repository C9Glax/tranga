using Database.DownloadContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Chapter;

/// <summary>
/// Downloads a chapter
/// </summary>
public abstract class PostDownloadChapter
{
    /// <summary>
    /// Downloads a chapter
    /// </summary>
    /// <param name="downloadContext"></param>
    /// <param name="chapterId">Id of the Chapter</param>
    /// <param name="ct"></param>
    /// <response code="200">Chapter will be downloaded</response>
    /// <response code="404">Chapter could not be found</response>
    public static async Task<Results<Ok, NotFound>> Handle(DownloadContext downloadContext, [FromRoute] Guid chapterId, CancellationToken ct)
    {
        if (await downloadContext.Chapters.FirstOrDefaultAsync(c => c.ChapterId == chapterId, ct) is not { } chapter)
            return TypedResults.NotFound();

        // TODO Download code
        
        return TypedResults.Ok();
    }
}