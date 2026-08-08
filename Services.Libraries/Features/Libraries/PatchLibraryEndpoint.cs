using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Services.Libraries.Database;

namespace Services.Libraries.Features.Libraries;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class PatchLibraryEndpoint
{
    /// <summary>
    /// Rename a library extension and/or rotate its credentials
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="libraryId">ID of the library</param>
    /// <param name="req">Request parameters</param>
    /// <param name="ct"></param>
    /// <response code="200">Library updated</response>
    /// <response code="400">Neither or both auth modes given, or the given credentials are invalid</response>
    /// <response code="404">Library with requested ID does not exist</response>
    public static async Task<Results<Ok, BadRequest<string>, NotFound>> Handle(LibrariesContext ctx, [FromRoute]Guid libraryId, [FromBody]PatchLibraryRequest req, CancellationToken ct)
    {
        DbLibraryService? existing = await ctx.LibraryServices.AsNoTracking().FirstOrDefaultAsync(l => l.LibraryServiceId == libraryId, ct);
        if (existing is null)
            return TypedResults.NotFound();

        string newName = req.Name ?? existing.Name;
        string newApiKey = existing.ApiKey;
        string? newUsername = existing.Username;

        bool hasApiKey = !string.IsNullOrEmpty(req.ApiKey);
        bool hasCredentials = !string.IsNullOrEmpty(req.Username) && !string.IsNullOrEmpty(req.Password);
        if (hasApiKey || hasCredentials)
        {
            KomgaAuthResolutionResult authResult = await KomgaAuthHelper.ResolveApiKey(existing.BaseUrl, req.ApiKey, req.Username, req.Password, ct);
            if (authResult.Error is { } error)
                return TypedResults.BadRequest(error);
            newApiKey = authResult.ApiKey!;
            newUsername = req.Username ?? existing.Username;
        }

        await ctx.LibraryServices.Where(l => l.LibraryServiceId == libraryId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Name, newName)
                .SetProperty(p => p.ApiKey, newApiKey)
                .SetProperty(p => p.Username, newUsername), ct);

        return TypedResults.Ok();
    }

    public sealed record PatchLibraryRequest
    {
        public string? Name { get; init; }
        public string? ApiKey { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }
    }
}
