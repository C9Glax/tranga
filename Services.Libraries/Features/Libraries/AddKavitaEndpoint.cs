using Extensions.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Services.Libraries.Database;
using Services.Libraries.Helpers;
using Services.Manga.Database;

namespace Services.Libraries.Features.Libraries;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class AddKavitaEndpoint
{
    /// <summary>
    /// Add kavita library extension
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="mangaContext"></param>
    /// <param name="req">Request parameters</param>
    /// <param name="logger"></param>
    /// <param name="ct"></param>
    /// <returns>ID of the new library extension</returns>
    /// <response code="200">Kavita extension added</response>
    /// <response code="400">The credentials are invalid, the server is unreachable or the library does not exist on the server</response>
    public static async Task<Results<Ok<Guid>, BadRequest<string>>> Handle(LibrariesContext ctx, MangaContext mangaContext,
        [FromBody]AddKavitaLibraryRequest req, ILogger<AddKavitaEndpoint> logger, CancellationToken ct)
    {
        KavitaLibrary[] libraries;
        try
        {
            libraries = await new Kavita(req.BaseUrl, req.ApiKey).GetLibraries(ct);
        }
        catch (HttpRequestException)
        {
            return TypedResults.BadRequest("Could not reach the Kavita server, or the Auth Key is invalid.");
        }

        if (libraries.All(l => l.Id != req.KavitaLibraryId))
            return TypedResults.BadRequest($"Kavita has no library with ID {req.KavitaLibraryId}.");

        DbLibraryService dbLibraryService = new(LibraryServiceType.Kavita, req.Name, req.BaseUrl, req.ApiKey)
        {
            TrangaLibraryId = req.KavitaLibraryId
        };
        if (dbLibraryService.ToClient() is not { } client)
            return TypedResults.BadRequest("Unsupported library type.");

        await ctx.LibraryServices.AddAsync(dbLibraryService, ct);

        await SeriesLinker.LinkExistingMangaByName(ctx, mangaContext, dbLibraryService, client, logger, ct);

        await ctx.SaveChangesAsync(ct);
        return TypedResults.Ok(dbLibraryService.LibraryServiceId);
    }

    /// <summary>
    /// Request body for adding a Kavita library extension connection.
    /// </summary>
    public sealed record AddKavitaLibraryRequest
    {
        /// <summary>User-chosen display name for the connection.</summary>
        public required string Name { get; init; }
        /// <summary>Base URL of the Kavita server.</summary>
        public required string BaseUrl { get; init; }
        /// <summary>Kavita Auth Key (User Settings -> Manage Auth Keys).</summary>
        public required string ApiKey { get; init; }
        /// <summary>ID of the existing Kavita library whose folder Tranga downloads into.</summary>
        public required string KavitaLibraryId { get; init; }
    }
}
