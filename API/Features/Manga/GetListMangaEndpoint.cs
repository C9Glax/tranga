using API.DTOs;
using API.Helpers;
using Database.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

/// <summary>
/// Get a list of Manga
/// </summary>
public abstract class GetListMangaEndpoint
{
    /// <summary>
    /// Get a list of Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="includes">Additional Information to include</param>
    /// <param name="unmonitored">All known Manga</param>
    /// <param name="ct"></param>
    /// <returns>List of Manga</returns>
    /// <response code="200">List of Manga</response>
    public static async Task<Results<Ok<MangaDTO[]>, InternalServerError>> Handle(MangaContext mangaContext, [FromQuery]GetMangaEndpoint.Includes[] includes, [FromQuery]bool unmonitored, CancellationToken ct)
    {
        // TODO Pagination
        if (await mangaContext.Mangas
                .IncludeDownloadLinks(includes.Contains(GetMangaEndpoint.Includes.DownloadLinks))
                .IncludeMetadataLinks(includes.Contains(GetMangaEndpoint.Includes.MetadataLinks))
                .Where(m => unmonitored || m.Monitor)
                .ToListAsync(ct) is not { } mangas)
            return TypedResults.InternalServerError();
        
        return TypedResults.Ok(mangas.Select(m => m.ToDTO()).ToArray());
    }
}