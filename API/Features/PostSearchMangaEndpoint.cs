using API.DTOs;
using Common.Datatypes;
using Data;
using Database.MangaContext;
using Database.MangaContext.Helpers;
using MetadataExtensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace API.Features;

public abstract class PostSearchMangaEndpoint
{
    public static async Task<Results<Ok<MangaSearchResult[]>, BadRequest>> Handle(MangaContext mangaContext, [FromBody]SearchQuery query, CancellationToken ct)
    {
        if (await MetadataExtensionsCollection.MangaUpdates.Search(query, ct) is not { } searchResult)
            return TypedResults.BadRequest();
        
        List<ComicInfo> mergedResult = await mangaContext.MergeComicInfos(searchResult, ct);
        MangaSearchResult[] convertedResult = mergedResult.Select(ci => new MangaSearchResult()
        {
            Title = ci.Title
        }).ToArray();
        
        return TypedResults.Ok(convertedResult);
    }
}