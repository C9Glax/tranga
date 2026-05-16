using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services.Libraries.Database;

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
    public static async Task<Ok<Guid>> Handle(LibrariesContext ctx, [FromBody]AddKomgaLibraryRequest req, CancellationToken ct)
    {
        DbLibrary dbLibrary = new (LibraryType.Komga, req.Name, req.BaseUrl, req.ApiKey);
        await ctx.Libraries.AddAsync(dbLibrary, ct);
        await ctx.SaveChangesAsync(ct);
        return TypedResults.Ok(dbLibrary.Id);
    }

    public sealed record AddKomgaLibraryRequest
    {
        public string Name { get; init; }
        public string BaseUrl { get; init; }
        public string ApiKey { get; init; }
    }
}