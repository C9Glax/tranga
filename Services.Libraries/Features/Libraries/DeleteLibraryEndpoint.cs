using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Libraries.Database;

namespace Services.Libraries.Features.Libraries;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class DeleteLibraryEndpoint
{
    /// <summary>
    /// Remove a library extension
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="ct"></param>
    /// <response code="200">Extension removed</response>
    /// <response code="404">Extension with ID does not exist</response>
    public static async Task<Results<Ok, NotFound>> Handle(LibrariesContext ctx, [FromRoute]Guid libraryId, CancellationToken ct)
    {
        int deleted = await ctx.LibraryServices.Where(l => l.LibraryServiceId == libraryId).ExecuteDeleteAsync(ct);
        if (deleted < 1)
            return TypedResults.NotFound();
        return TypedResults.Ok();
    }
}