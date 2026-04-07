using API.Entities;
using API.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace API.Features.DownloadLinks;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetDownloadLinksEndpoint
{
    /// <summary>
    /// All Download-Links
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="ct"></param>
    /// <returns>List of all Download-Links</returns>
    /// <response code="200">List of all Download-Links</response>
    public static async Task<Results<Ok<MangaDownloadLink[]>, InternalServerError>> Handle(MangaContext mangaContext,  CancellationToken ct)
    {
        if (await mangaContext.MangaDownloadLinks.ToListAsync(ct) is not { } list)
            return TypedResults.InternalServerError();
        
        //TODO Pagination

        MangaDownloadLink[] result = list.Select(e => e.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}