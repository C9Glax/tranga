using Services.Manga.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;

namespace Services.Manga.Features.Metadata;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetMetadataEntriesEndpoint
{
    /// <summary>
    /// List of all Metadata-Entries
    /// </summary>
    /// <param name="mangaContext"></param>
    /// <param name="ct"></param>
    /// <returns>A list of all Metadata-Entries</returns>
    /// <response code="200">A list of all Metadata-Entries</response>
    public static async Task<Results<Ok<Entities.Metadata[]>, InternalServerError>> Handle(MangaContext mangaContext, CancellationToken ct)
    {
        if (await mangaContext.MetadataEntries.ToListAsync(ct) is not { } list)
            return TypedResults.InternalServerError();
        
        //TODO Pagination

        Entities.Metadata[] result = list.Select(e => e.ToDTO()).ToArray();
        return TypedResults.Ok(result);
    }
}