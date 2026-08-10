using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Libraries.Database;

namespace Services.Libraries.Features.Libraries;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetLibraryMangaLinkEndpoint
{
    /// <summary>
    /// Get the library-extension links (e.g. Komga series) for a Manga
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="mangaId">ID of Manga in the Manga service</param>
    /// <param name="ct"></param>
    /// <returns>Array of library links for the Manga, empty when none exist</returns>
    /// <response code="200">Array of library links for the Manga</response>
    public static async Task<Ok<LibraryMangaLink[]>> Handle(LibrariesContext ctx, [FromRoute]Guid mangaId, CancellationToken ct)
    {
        LibraryMangaLink[] links = await ctx.MangaMappings
            .Where(m => m.MangaId == mangaId)
            .Join(ctx.LibraryServices, m => m.LibraryServiceId, l => l.LibraryServiceId, (m, l) => new LibraryMangaLink(
                l.LibraryServiceId,
                // Assumption: Komga's web UI series route is "{baseUrl}/series/{seriesId}" - verify against a real Komga instance.
                $"{l.BaseUrl.TrimEnd('/')}/series/{m.SeriesId}"))
            .ToArrayAsync(ct);

        return TypedResults.Ok(links);
    }

    /// <summary>
    /// A link from a Manga to its corresponding series page on a configured library extension.
    /// </summary>
    /// <param name="LibraryServiceId">Id of the library connection the series belongs to.</param>
    /// <param name="SeriesUrl">URL of the series on the library extension's web UI.</param>
    public sealed record LibraryMangaLink(Guid LibraryServiceId, string SeriesUrl);
}
