using API.DTOs;
using Common.Datatypes;
using Common.Helpers;
using Database.MangaContext;
using Database.MangaContext.Helpers;
using MetadataExtensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ComicInfo = MetadataExtensions.ComicInfo;

namespace API.Features;

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
    public static async Task<Results<Ok<MangaSearchResult[]>, InternalServerError>> Handle(MangaContext mangaContext, [FromBody]SearchQuery query, CancellationToken ct)
    {
        List<ComicInfo> searchResult = MetadataExtensionsCollection.SearchAll(query, ct);
        
        await mangaContext.InsertNewDataIntoContext(searchResult, ct);
        
        MangaSearchResult[] convertedResult = searchResult.Select(ci => new MangaSearchResult()
        {
            Title = ci.Title,
            Description = ci.Summary,
            CoverBase64 = ci.Cover.ToCoverBase64(),
            Url = ci.Web
        }).ToArray();
        
        return TypedResults.Ok(convertedResult);
    }
}