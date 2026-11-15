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
        List<DbMangaIdMapping> mappings = await ctx.MangaMappings.Where(m => m.MangaId == mangaId).ToListAsync(ct);
        Guid[] libraryIds = mappings.Select(m => m.LibraryServiceId).Distinct().ToArray();
        Dictionary<Guid, DbLibraryService> libraries = await ctx.LibraryServices
            .Where(l => libraryIds.Contains(l.LibraryServiceId))
            .ToDictionaryAsync(l => l.LibraryServiceId, ct);

        LibraryMangaLink[] links = mappings
            .Where(m => libraries.ContainsKey(m.LibraryServiceId))
            .Select(m => new LibraryMangaLink(m.LibraryServiceId, SeriesUrl(libraries[m.LibraryServiceId], m.SeriesId)))
            .ToArray();

        return TypedResults.Ok(links);
    }

    private static string SeriesUrl(DbLibraryService library, string seriesId)
    {
        string baseUrl = library.BaseUrl.TrimEnd('/');
        return library.LibraryServiceType switch
        {
            // Kavita's web UI series route is "{baseUrl}/library/{libraryId}/series/{seriesId}".
            LibraryServiceType.Kavita => $"{baseUrl}/library/{library.TrangaLibraryId}/series/{seriesId}",
            // Assumption: Komga's web UI series route is "{baseUrl}/series/{seriesId}" - verify against a real Komga instance.
            _ => $"{baseUrl}/series/{seriesId}"
        };
    }

    /// <summary>
    /// A link from a Manga to its corresponding series page on a configured library extension.
    /// </summary>
    /// <param name="LibraryServiceId">Id of the library connection the series belongs to.</param>
    /// <param name="SeriesUrl">URL of the series on the library extension's web UI.</param>
    public sealed record LibraryMangaLink(Guid LibraryServiceId, string SeriesUrl);
}
