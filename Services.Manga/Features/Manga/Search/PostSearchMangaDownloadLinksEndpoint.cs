using Common.Datatypes;
using Services.Manga.Helpers;
using Extensions;
using Extensions.Data;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Database.Helpers;
using Services.Manga.Entities;

namespace Services.Manga.Features.Manga.Search;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class PostSearchMangaDownloadLinksEndpoint
{
    /// <summary>
    /// Search Manga on Download-Extensions
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="mangaId">ID of Manga to Search</param>
    /// <param name="req"></param>
    /// <param name="ct"></param>
    /// <returns>Search result</returns>
    /// <response code="200">Search result</response>
    /// <response code="404">Manga with ID does not exist</response>
    public static async Task<Results<Ok<MangaDownloadLink[]>, NotFound, InternalServerError>> Handle(MangaContext mangaContext, [FromRoute] Guid mangaId, [FromBody] PostSearchMangaDownloadLinksRequest? req, CancellationToken ct)
    {
        if (await mangaContext.GetManga(mangaId, ct) is not { } source)
            return TypedResults.NotFound();

        SearchQuery query = req?.SearchTerm is { Length: > 0 } term
            ? source.ToSearchQuery() with { Title = term }
            : source.ToSearchQuery();

        List<MangaInfo> searchResult = DownloadExtensionsCollection.SearchAll(query, ct);

        if (await mangaContext.MangaDownloadLinks.Where(m => m.MangaId == mangaId).ToListAsync(ct) is not { } existingSources)
            return TypedResults.InternalServerError();

        List<DbMangaDownloadLinks> result = [];
        foreach (MangaInfo mangaInfo in searchResult)
        {
            if (existingSources.FirstOrDefault(d =>
                    d.DownloadLink.DownloadExtension == mangaInfo.ExtensionIdentifier && d.DownloadLink.Identifier == mangaInfo.Identifier)
                is not { } existing)
            {
                DbDownloadLink downloadLink = new()
                {
                    DownloadExtension = mangaInfo.ExtensionIdentifier,
                    Identifier = mangaInfo.Identifier,
                    Series = mangaInfo.Title,
                    Summary = mangaInfo.Description,
                    Url = mangaInfo.Url,
                    NSFW = mangaInfo.NSFW
                };

                DbMangaDownloadLinks mangaDownloadLinks = new()
                {
                    DownloadLink = downloadLink,
                    Manga = source.Manga,
                    Matched = false,
                    Priority = 0
                };

                await mangaContext.AddAsync(mangaDownloadLinks, ct);
                await mangaContext.SaveCover(downloadLink, mangaInfo.Cover, ct);

                result.Add(mangaDownloadLinks);
            }
            else
            {
                // A link that was added by its URL when no cover could be fetched has none: the search found the
                // series, so it can have the cover of the source.
                if (existing.DownloadLink.CoverId is null)
                    await mangaContext.SaveCover(existing.DownloadLink, mangaInfo.Cover, ct);

                result.Add(existing);
            }
        }

        await mangaContext.SaveChangesAsync(ct);

        return TypedResults.Ok(result.Select(r => r.ToDTO()).ToArray());
    }

    /// <summary>
    /// Used in <see cref="PostSearchMangaDownloadLinksEndpoint"/>
    /// </summary>
    /// <param name="SearchTerm">Optional free-text override for the search term. Falls back to the manga's stored title when null/empty.</param>
    public sealed record PostSearchMangaDownloadLinksRequest(string? SearchTerm);
}
