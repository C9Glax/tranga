using Common.Helpers;
using Extensions.Data;
using Extensions.Extensions;
using Komga.Client.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Services.Libraries.Database;
using Services.Libraries.Helpers;
using Services.Manga.Database;

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
    /// <param name="mangaContext"></param>
    /// <param name="req">Request parameters</param>
    /// <param name="logger"></param>
    /// <param name="ct"></param>
    /// <returns>200 OK if Komga extension added</returns>
    /// <response code="200">Komga extension added</response>
    /// <response code="400">Neither or both auth modes given, or the given credentials are invalid</response>
    public static async Task<Results<Ok<Guid>, BadRequest<string>>> Handle(LibrariesContext ctx, MangaContext mangaContext,
        [FromBody]AddKomgaLibraryRequest req, ILogger<AddKomgaEndpoint> logger, CancellationToken ct)
    {
        KomgaAuthResolutionResult authResult = await KomgaAuthHelper.ResolveApiKey(req.BaseUrl, req.ApiKey, req.Username, req.Password, ct);
        if (authResult.Error is { } error)
            return TypedResults.BadRequest(error);

        DbLibraryService dbLibraryService = new (LibraryServiceType.Komga, req.Name, req.BaseUrl, authResult.ApiKey!)
        {
            Username = req.Username
        };
        if (dbLibraryService.ToExtension() is not { } extension)
            return TypedResults.BadRequest("Unsupported library type.");
        dbLibraryService.TrangaLibraryId = await extension.CreateTrangaLibrary(ct, req.libraryRootPath);

        await ctx.LibraryServices.AddAsync(dbLibraryService, ct);

        await LinkExistingMangaByName(ctx, mangaContext, dbLibraryService, extension, logger, ct);

        await ctx.SaveChangesAsync(ct);
        return TypedResults.Ok(dbLibraryService.LibraryServiceId);
    }

    /// <summary>
    /// Links every Tranga manga to a Komga series on a name-equality basis (the Komga series name matches
    /// the manga's on-disk directory name), and pushes metadata for each newly created link. Runs once
    /// when a Komga library is first connected, so pre-existing manga get linked and synced immediately
    /// instead of waiting on their next chapter download.
    /// </summary>
    private static async Task LinkExistingMangaByName(LibrariesContext ctx, MangaContext mangaContext, DbLibraryService dbLibraryService,
        Extensions.Extensions.Komga extension, ILogger<AddKomgaEndpoint> logger, CancellationToken ct)
    {
        KomgaSeries[] seriesList = await extension.GetSeriesList(ct);
        List<DbMangaMetadataEntries> mangaEntries = await mangaContext.MangaMetadataEntries
            .Where(e => e.Chosen == true)
            .ToListAsync(ct);

        foreach (DbMangaMetadataEntries entry in mangaEntries)
        {
            string expectedName = entry.Metadata.Series.SafeFilesystemString();
            KomgaSeries? match = seriesList.FirstOrDefault(s => s.Name == expectedName);
            if (match is null)
                continue;

            try
            {
                await ctx.MangaMappings.AddAsync(new DbMangaIdMapping(dbLibraryService.LibraryServiceId, entry.MangaId, match.Id), ct);
                await KomgaMetadataSync.PushMetadata(mangaContext, extension, match.Id, entry.MangaId, ct);
            }
            catch (Exception e)
            {
                logger.LogWarning(e, "Failed to link/push metadata for manga {MangaId} to Komga series {SeriesId} on connect",
                    entry.MangaId, match.Id);
            }
        }
    }

    public sealed record AddKomgaLibraryRequest
    {
        public required string Name { get; init; }
        public required string BaseUrl { get; init; }
        public string? ApiKey { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }

        public string? libraryRootPath { get; init; }
    }
}

/// <summary>
/// Result of resolving either a directly-supplied API key or a minted-from-credentials API key.
/// </summary>
internal sealed record KomgaAuthResolutionResult(string? ApiKey, string? Error);

/// <summary>
/// Shared auth-mode resolution logic used by <see cref="AddKomgaEndpoint"/>, <see cref="TestKomgaConnectionEndpoint"/>
/// and <see cref="PatchLibraryEndpoint"/>: exactly one of "ApiKey" or "Username"+"Password" must be supplied.
/// </summary>
internal static class KomgaAuthHelper
{
    public static async Task<KomgaAuthResolutionResult> ResolveApiKey(string baseUrl, string? apiKey, string? username, string? password, CancellationToken ct)
    {
        bool hasApiKey = !string.IsNullOrEmpty(apiKey);
        bool hasCredentials = !string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password);

        if (hasApiKey == hasCredentials)
            return new KomgaAuthResolutionResult(null, "Provide exactly one of ApiKey or (Username and Password).");

        if (hasApiKey)
            return new KomgaAuthResolutionResult(apiKey, null);

        try
        {
            string mintedApiKey = await Extensions.Extensions.Komga.MintApiKey(baseUrl, username!, password!, ct);
            return new KomgaAuthResolutionResult(mintedApiKey, null);
        }
        catch (ApiException)
        {
            return new KomgaAuthResolutionResult(null, "Invalid Komga username or password.");
        }
        catch (HttpRequestException)
        {
            return new KomgaAuthResolutionResult(null, "Could not reach the Komga server.");
        }
    }
}
