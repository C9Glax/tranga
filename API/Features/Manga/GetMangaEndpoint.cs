using API.DTOs;
using API.Helpers;
using Database.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

/// <summary>
/// Get a Manga
/// </summary>
public abstract class GetMangaEndpoint
{
    /// <summary>
    /// Get a Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of the Manga</param>
    /// <param name="includes">Additional Information to include</param>
    /// <param name="ct"></param>
    /// <returns>Info of the Manga</returns>
    /// <response code="200">Info of the Manga</response>
    /// <response code="404">Manga could not be found</response>
    public static async Task<Results<Ok<MangaDTO>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, [FromQuery]Includes[] includes, CancellationToken ct)
    {
        if (await mangaContext.Mangas
                .IncludeDownloadLinks(includes.Contains(Includes.DownloadLinks))
                .IncludeMetadataLinks(includes.Contains(Includes.MetadataLinks))
                .FirstOrDefaultAsync(m => m.Id == mangaId, ct) is not { } manga)
            return TypedResults.NotFound();
        
        return TypedResults.Ok(manga.ToDTO());
    }
    
    /// <summary>
    /// Additional information to include in the response
    /// </summary>
    public enum Includes {
        /// <summary>
        /// Include <see cref="DownloadLinkDTO"/> 
        /// </summary>
        DownloadLinks,
        /// <summary>
        /// Include <see cref="MetadataLinkDTO"/> 
        /// </summary>
        MetadataLinks
    }
}
