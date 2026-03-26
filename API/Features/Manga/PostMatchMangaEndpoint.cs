using API.DTOs;
using Common.Datatypes;
using Database.MangaContext;
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
    /// <param name="mangaId">ID of the Manga</param>
    /// <param name="ct"></param>
    /// <returns>A List of matched Manga</returns>
    /// <response code="200">A List of matched Manga</response>
    /// <response code="404"> could not be found</response>
    public static async Task<Results<Ok<MangaMatchResultDTO[]>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.Mangas.Include(m => m.DownloadLinks).FirstOrDefaultAsync(m => m.Id == mangaId, ct) is not { } manga)
            return TypedResults.NotFound();

        // TODO Add search-by options
        SearchQuery searchQuery = new ()
        {
            Title = manga.Series
        };
        List<MangaInfo> searchResult = DownloadExtensionsCollection.SearchAll(searchQuery, ct);

        MangaMatchResultDTO[] result = await InsertNewDataIntoMangaContext(mangaContext, manga, searchResult, ct); 

        return TypedResults.Ok(result);
    }

    private static async Task<MangaMatchResultDTO[]> InsertNewDataIntoMangaContext(MangaContext mangaContext, DbManga manga, List<MangaInfo> searchResult, CancellationToken ct)
    {
        Dictionary<DbDownloadLink, MangaInfo> existing = (await mangaContext.Mangas.Include(m => m.DownloadLinks).SelectMany(m => m.DownloadLinks!)
                .Where(l => searchResult.Any(sr =>
                    sr.ExtensionIdentifier == l.DownloadExtensionId && sr.Identifier == l.Identifier))
                .ToListAsync(ct))
            .ToDictionary(l => l,
                l => searchResult.First(sr =>
                    sr.ExtensionIdentifier == l.DownloadExtensionId && sr.Identifier == l.Identifier));

        List<MangaInfo> missing = searchResult.Where(sr => !existing.ContainsValue(sr)).ToList();
        
        foreach (MangaInfo mangaInfo in missing)
        {
            DbDownloadLink link = new ()
            {
                MangaId = manga.Id,
                DownloadExtensionId = mangaInfo.ExtensionIdentifier,
                Identifier = mangaInfo.Identifier,
                Url = mangaInfo.Url
            };
            manga.DownloadLinks!.Add(link);
            existing.Add(link, mangaInfo);
        }

        return existing.Select(kv => new MangaMatchResultDTO()
        {
            MangaId = manga.Id,
            Description = kv.Value.Description,
            DownloadId = kv.Key.Id,
            Title = kv.Value.Title,
            Url = kv.Value.Url
        }).ToArray();
    }
}