using API.DTOs;
using Common.Datatypes;
using Common.Helpers;
using Database.MangaContext;
using Database.MangaContext.Helpers;
using DownloadExtensions;
using DownloadExtensions.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

/// <summary>
/// Matches the Manga on all DownloadExtensions
/// </summary>
public abstract class PostMatchMangaEndpoint
{
    /// <summary>
    /// Matches the Manga on all DownloadExtensions
    /// </summary>
    /// <remarks><see cref="PostSearchMangaEndpoint"/></remarks>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">The ID of the Manga to match</param>
    /// <param name="ct"></param>
    /// <returns>A List of matched Manga</returns>
    /// <response code="200">A List of matched Manga</response>
    /// <response code="404">Manga could not be found</response>
    public static async Task<Results<Ok<MangaSearchResultDTO[]>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.FilterManga(mangaId).FirstOrDefaultAsync(ct) is not { } manga)
            return TypedResults.NotFound();

        SearchQuery searchQuery = manga.ToSearchQuery();
        List<MangaInfo> searchResult = DownloadExtensionsCollection.SearchAll(searchQuery, ct);
        
        // TODO insert into db
        
        MangaSearchResultDTO[] convertedResult = searchResult.Select(mi => new MangaSearchResultDTO()
        {
            Title = mi.Title,
            Description = mi.Description ?? string.Empty,
            Url = mi.Url,
            MangaId = mangaId
        }).ToArray();

        return TypedResults.Ok(convertedResult);
    }
}