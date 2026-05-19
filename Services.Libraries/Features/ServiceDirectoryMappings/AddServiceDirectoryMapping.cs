using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Libraries.Database;

namespace Services.Libraries.Features.ServiceDirectoryMappings;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class AddServiceDirectoryMapping
{
    
    /// <summary>
    /// Add a service directory mapping. <see cref="DbServiceDirectoryMapping"/>
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="req">Request parameters</param>
    /// <param name="ct"></param>
    /// <response code="200">Mapping added</response>
    public static async Task<Ok<Guid>> Handle(LibrariesContext ctx, [FromRoute]Guid libraryId, [FromBody]AddServiceDirectoryMappingRequest req, CancellationToken ct)
    {
        DbServiceDirectoryMapping mapping = new (libraryId, req.TrangaPath, req.ServicePath);
        await ctx.AddAsync(mapping, ct);
        await ctx.SaveChangesAsync(ct);
        return TypedResults.Ok(mapping.MappingId);
    }

    public sealed record AddServiceDirectoryMappingRequest
    {
        public required string TrangaPath { get; init; }
        public required string ServicePath { get; init; }
    }
}