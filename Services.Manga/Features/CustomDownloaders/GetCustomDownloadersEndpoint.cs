using Extensions.Custom;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Helpers;

namespace Services.Manga.Features.CustomDownloaders;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetCustomDownloadersEndpoint
{
    /// <summary>
    /// List of all custom (selector based) Download-Sources
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="ct"></param>
    /// <response code="200">The definitions of all custom Download-Sources, sorted by name</response>
    public static async Task<Ok<List<SelectorDownloaderDefinition>>> Handle([FromServices] MangaContext mangaContext, CancellationToken ct)
    {
        List<DbCustomDownloader> rows = await mangaContext.CustomDownloaders.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);
        return TypedResults.Ok(rows.Select(CustomDownloaderStore.ToDefinition).OfType<SelectorDownloaderDefinition>().ToList());
    }
}
