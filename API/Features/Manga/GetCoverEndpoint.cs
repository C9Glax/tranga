using Common.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace API.Features.Manga;

/// <summary>
/// Get a Manga Cover
/// </summary>
public abstract class GetCoverEndpoint
{
    /// <summary>
    /// Get a Manga Cover
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">Id of the manga</param>
    /// <param name="ct"></param>
    /// <returns>The Cover-Image</returns>
    /// <response code="200">The Cover-Image</response>
    /// <response code="404">Cover does not exist or could not be loaded</response>
    public static async Task<Results<FileStreamHttpResult, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        if (await MangaCover.LoadCover(mangaId.ToString(), ct) is not { } memoryStream)
            return TypedResults.NotFound();

        return TypedResults.File(memoryStream, "image/png", mangaId.ToString());
    }
}