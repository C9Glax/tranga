using API.Helpers;
using Database.MangaContext;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace API.Features.Manga;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetMangaListEndpoint
{
    /// <summary>
    /// List of all Manga
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="ct"></param>
    /// <returns>List of all Manga</returns>
    /// <response code="200">List of all Manga</response>
    public static async Task<Results<Ok<Entities.Manga[]>, InternalServerError>> Handle(MangaContext mangaContext, CancellationToken ct)
    {
        if (await mangaContext.MangaMetadataEntries
                .Where(s => s.Chosen == true)
                .ToListAsync(ct) is not { } metadataSources)
        {
            return TypedResults.InternalServerError();
        }

        Entities.Manga[] result = metadataSources.Select(m => m.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}