using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Libraries.Database;

namespace Services.Libraries.Features.ServiceDirectoryMappings;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class DeleteServiceDirectoryMapping
{
    
    /// <summary>
    /// Delete a service directory mapping. <see cref="DbServiceDirectoryMapping"/>
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="ct"></param>
    /// <response code="200">Mapping added</response>
    public static async Task<Results<Ok, NotFound>> Handle(LibrariesContext ctx, [FromRoute]Guid mappingId, CancellationToken ct)
    {
        int count = await ctx.DirectoryMappings.Where(m => m.MappingId == mappingId).ExecuteDeleteAsync(ct);
        if (count < 1)
            return TypedResults.NotFound();
        return TypedResults.Ok();
    }
}