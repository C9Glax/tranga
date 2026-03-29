using API.DTOs;
using Common.Datatypes;
using Common.Helpers;
using Database;
using Database.Helpers;
using Database.MangaContext;
using MetadataExtensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Settings;

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
        List<SearchResult> searchResult = MetadataExtensionsCollection.SearchAll(query, ct);
        
        MangaSearchResultDTO[] result = await InsertNewDataIntoMangaContext(mangaContext, searchResult, ct);

        await mangaContext.SaveChangesAsync(ct);
        
        return TypedResults.Ok(result);
    }

    private static async Task<MangaSearchResultDTO[]> InsertNewDataIntoMangaContext(MangaContext mangaContext, List<SearchResult> searchResults, CancellationToken ct)
    {
        List<MangaSearchResultDTO> ret = new ();
        foreach (SearchResult searchResult in searchResults)
        {
            if (await mangaContext.Mangas
                    .Include(m => m.MetadataLinks)
                    .Select(m => new
                    { 
                        Manga = m, MetadataLink = m.MetadataLinks!.FirstOrDefault(l =>
                            l.MetadataExtensionId == searchResult.MetadataExtensionIdentifier &&
                            l.Identifier == searchResult.Identifier)
                    })
                    .FirstOrDefaultAsync(ct) is { Manga: not null, MetadataLink: not null } existing)
            {
                ret.Add(CreateMangaSearchResultDTO(existing.Manga, existing.MetadataLink));
            } else if (await mangaContext.Mangas
                           .Include(m => m.MetadataLinks)
                           .FirstOrDefaultAsync(m => m.Series == searchResult.Series, ct) is { } dbManga)
            {
                DbMetadataLink link = await CreateDbMetadataLink(mangaContext, dbManga, searchResult, ct);
                ret.Add(CreateMangaSearchResultDTO(dbManga, link));
            }
            else
            {
                DbManga manga = await CreateDbManga(mangaContext, searchResult, ct);
                DbMetadataLink link = await CreateDbMetadataLink(mangaContext, manga, searchResult, ct);
                ret.Add(CreateMangaSearchResultDTO(manga, link));
            }
        }

        return ret.ToArray();
    }

    private static async Task<DbManga> CreateDbManga(MangaContext mangaContext, SearchResult searchResult, CancellationToken ct)
    {
        DbManga manga = new ()
        {
            Series = searchResult.Series,
            Monitor = false
        };
        await mangaContext.Mangas.AddAsync(manga, ct);
        return manga;
    }

    private static async Task<DbMetadataLink> CreateDbMetadataLink(MangaContext mangaContext, DbManga manga, SearchResult searchResult, CancellationToken ct)
    {
        DbMetadataLink link = new()
        {
            Manga = manga,
            MangaId = manga.Id,
            MetadataExtensionId = searchResult.MetadataExtensionIdentifier,
            Identifier = searchResult.Identifier,
            Year = searchResult.Year,
            Artists = searchResult.Artists,
            Authors = searchResult.Authors,
            Genres = searchResult.Genres,
        };
        await mangaContext.AddAsync(link, ct);
        await link.SaveCover(mangaContext, searchResult.Cover, ct);
        return link;
    }

    private static MangaSearchResultDTO CreateMangaSearchResultDTO(DbManga manga, DbMetadataLink link) => new()
    {
        MangaId = manga.Id,
        MetadataId = link.Id,
        Title = manga.Series,
        CoverFileId = link.CoverId,
        Description = link.Summary,
        Url = link.Url
    };
}