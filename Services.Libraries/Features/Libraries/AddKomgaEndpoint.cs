using Extensions.Data;
using Extensions.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Libraries.Database;
using Services.Libraries.Helpers;

namespace Services.Libraries.Features.Libraries;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class AddKomgaEndpoint
{
    /// <summary>
    /// Add komga library extension
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="req">Request parameters</param>
    /// <param name="ct"></param>
    /// <returns>200 OK if Komga extension added</returns>
    /// <response code="200">Komga extension added</response>
    public static async Task<Results<Ok<Guid>, BadRequest>> Handle(LibrariesContext ctx, [FromBody]AddKomgaLibraryRequest req, CancellationToken ct)
    {
        DbLibraryService dbLibraryService = new (LibraryServiceType.Komga, req.Name, req.BaseUrl, req.ApiKey);
        if (dbLibraryService.ToExtension() is not { } extension)
            return TypedResults.BadRequest();
        dbLibraryService.TrangaLibraryId = await extension.CreateTrangaLibrary(ct, req.libraryRootPath);
        
        await ctx.LibraryServices.AddAsync(dbLibraryService, ct);
        await ctx.SaveChangesAsync(ct);
        return TypedResults.Ok(dbLibraryService.LibraryServiceId);
    }

    public sealed record AddKomgaLibraryRequest
    {
        public required string Name { get; init; }
        public required string BaseUrl { get; init; }
        public required string ApiKey { get; init; }
        
        public string? libraryRootPath { get; init; }
    }
}