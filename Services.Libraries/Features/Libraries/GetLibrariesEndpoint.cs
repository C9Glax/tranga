using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Libraries.Database;
using Services.Libraries.Entities;

namespace Services.Libraries.Features.Libraries;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetLibrariesEndpoint
{

    /// <summary>
    /// Gets all configured libraries extensions
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="ct"></param>
    /// <returns>List of all configured library extensions</returns>
    /// <response code="200">List of all configured library extensions</response>
    public static async Task<Ok<Library[]>> Handle(LibrariesContext ctx, CancellationToken ct)
    {
        List<DbLibraryService> dbLibraries = await ctx.LibraryServices.ToListAsync(ct);
        Library[] result = dbLibraries.Select(l => new Library(l.LibraryServiceType, l.LibraryServiceId, l.BaseUrl)).ToArray();
        return TypedResults.Ok(result);
    }
}