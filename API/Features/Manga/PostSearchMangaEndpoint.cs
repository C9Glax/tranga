using API.DTOs;
using Common.Datatypes;
using Database.MangaContext;
using Database.MangaContext.Helpers;
using MetadataExtensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ComicInfo = MetadataExtensions.ComicInfo;

namespace API.Features.Manga;

/// <summary>
/// Searches for a Manga
/// </summary>
public abstract class PostSearchMangaEndpoint
{
    /// <summary>
    /// Searches for a Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="query"></param>
    /// <param name="ct"></param>
    /// <returns>The Search-result</returns>
    /// <response code="200">The Search-result</response>
    /// <response code="500">Error while searching for Manga</response>
    public static async Task<Results<Ok<MangaSearchResultDTO[]>, InternalServerError>> Handle(MangaContext mangaContext, [FromBody]SearchQuery query, CancellationToken ct)
    {
        List<ComicInfo> searchResult = MetadataExtensionsCollection.SearchAll(query, ct);
        
        List<(DbManga manga, string url)> result = await mangaContext.InsertNewDataIntoContext(searchResult, ct);
        
        MangaSearchResultDTO[] convertedResult = result.Select(ci => new MangaSearchResultDTO()
        {
            MangaId = ci.manga.MangaId,
            Title = ci.manga.Title,
            Description = ci.manga.Description ?? null,
            CoverBase64 = ci.manga.CoverImageBase64,
            Url = ci.url
        }).ToArray();
        
        return TypedResults.Ok(convertedResult);
    }
}