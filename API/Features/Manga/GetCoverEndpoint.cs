using API.Helpers;
using Common.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
    /// <param name="mangaId">ID of the manga</param>
    /// <param name="metadataExtensionId">(optional) ID of the metadataExtension to retrieve the cover from</param>
    /// <param name="ct"></param>
    /// <returns>The Cover-Image</returns>
    /// <response code="200">The Cover-Image</response>
    /// <response code="204">Cover does not exist</response>
    /// <response code="404">Manga does not exist</response>
    /// <response code="500">Cover could not be loaded</response>
    public static async Task<Results<FileStreamHttpResult, NoContent, NotFound, InternalServerError>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, [FromQuery]Guid? metadataExtensionId, CancellationToken ct)
    {
        IQueryable<DbManga> queryable = mangaContext.Mangas.GetManga(mangaId);
        IQueryable<DbMetadataLink> metadataQuery = metadataExtensionId is { } id
            ? queryable.Include(m => m.MetadataLinks).GetMetadataLink(id)
            : queryable.Include(m => m.MetadataLinks).SelectMany(l => l.MetadataLinks!);
        
        if(await metadataQuery.ToListAsync(ct) is not { } metadataList)
            return TypedResults.NotFound();

        if (metadataList.FirstOrDefault() is not { } metadata)
            return TypedResults.NoContent();
        
        if (await mangaContext.Files.FirstOrDefaultAsync(f => f.Id == metadata.CoverId, ct) is not { } cover)
            return TypedResults.NoContent();
        
        if (await MangaCover.LoadCover(cover.Name, ct) is not { } memoryStream)
            return TypedResults.InternalServerError();

        return TypedResults.File(memoryStream, "image/png", mangaId.ToString());
    }
}