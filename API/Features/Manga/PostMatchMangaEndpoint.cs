using API.DTOs;
using API.Helpers;
using Common.Datatypes;
using Database.Helpers;
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
    /// <response code="404">Manga could not be found</response>
    public static async Task<Results<Ok<DownloadLinkDTO[]>, NotFound>> Handle(MangaContext mangaContext, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        if (await mangaContext.Mangas.Include(m => m.DownloadLinks).FirstOrDefaultAsync(m => m.Id == mangaId, ct) is not { } manga)
            return TypedResults.NotFound();

        // TODO Add search-by options
        SearchQuery searchQuery = new ()
        {
            Title = manga.Series
        };
        List<MangaInfo> searchResult = DownloadExtensionsCollection.SearchAll(searchQuery, ct);

        DownloadLinkDTO[] result = await InsertNewDataIntoMangaContext(mangaContext, manga, searchResult, ct);

        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok(result);
    }

    private static async Task<DownloadLinkDTO[]> InsertNewDataIntoMangaContext(MangaContext mangaContext, DbManga manga, List<MangaInfo> searchResults, CancellationToken ct)
    {
        List<DownloadLinkDTO> ret = new();

        await mangaContext.Entry(manga).Collection(m => m.DownloadLinks!).LoadAsync(ct);

        foreach (MangaInfo mangaInfo in searchResults)
        {
            if (manga.DownloadLinks!.FirstOrDefault(l =>
                    l.DownloadExtensionId == mangaInfo.ExtensionIdentifier && l.Identifier == mangaInfo.Identifier) is
                { } existing)
            {
                ret.Add(existing.ToDTO());
            }
            else
            {
                DbDownloadLink newLink = new ()
                {
                    Description = mangaInfo.Description,
                    DownloadExtensionId = mangaInfo.ExtensionIdentifier,
                    Identifier = mangaInfo.Identifier,
                    MangaId = manga.Id,
                    Manga = manga,
                    Title = mangaInfo.Title,
                    Url = mangaInfo.Url,
                    Matched = false
                };
                await mangaContext.AddAsync(newLink, ct);
                await newLink.SaveCover(mangaContext, mangaInfo.Cover, ct);
                ret.Add(newLink.ToDTO());
            }
        }

        return ret.ToArray();
    }
}