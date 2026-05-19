using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Libraries.Database;
using Services.Libraries.Entities;

namespace Services.Libraries.Features.ServiceDirectoryMappings;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetServiceDirectoryMappings
{
    
    /// <summary>
    /// Get directory mapping for a library service. <see cref="DbServiceDirectoryMapping"/>
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="ct"></param>
    /// <response code="200">List of all mappings</response>
    public static async Task<Ok<ServiceDirectoryMapping[]>> Handle(LibrariesContext ctx, [FromRoute]Guid libraryId, CancellationToken ct)
    {
        List<DbServiceDirectoryMapping> mappings = await ctx.DirectoryMappings.Where(m => m.LibraryId == libraryId).ToListAsync(ct);
        ServiceDirectoryMapping[] result = mappings.Select(m => new ServiceDirectoryMapping(m.MappingId, m.LibraryId, m.TrangaPath, m.ServicePath))
            .ToArray();
        return TypedResults.Ok(result);
    }
}