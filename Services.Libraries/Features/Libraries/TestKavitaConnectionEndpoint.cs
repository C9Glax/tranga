using Extensions.Extensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Services.Libraries.Features.Libraries;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class TestKavitaConnectionEndpoint
{
    /// <summary>
    /// Validates Kavita credentials without persisting anything and lists the libraries on the server
    /// </summary>
    /// <param name="req">Request parameters</param>
    /// <param name="ct"></param>
    /// <returns>The libraries on the Kavita server</returns>
    /// <response code="200">Credentials are valid; the Kavita libraries a connection can be bound to</response>
    /// <response code="401">The server is unreachable or the credentials are invalid</response>
    public static async Task<Results<Ok<KavitaLibraryInfo[]>, UnauthorizedHttpResult>> Handle([FromBody]TestKavitaConnectionRequest req, CancellationToken ct)
    {
        try
        {
            Kavita kavita = new(req.BaseUrl, req.ApiKey);
            KavitaLibrary[] libraries = await kavita.GetLibraries(ct);
            return TypedResults.Ok(libraries.Select(l => new KavitaLibraryInfo(l.Id, l.Name)).ToArray());
        }
        catch (HttpRequestException)
        {
            return TypedResults.Unauthorized();
        }
    }

    /// <summary>
    /// Request body for validating Kavita connection credentials.
    /// </summary>
    public sealed record TestKavitaConnectionRequest
    {
        /// <summary>Base URL of the Kavita server.</summary>
        public required string BaseUrl { get; init; }
        /// <summary>Kavita Auth Key (User Settings -> Manage Auth Keys).</summary>
        public required string ApiKey { get; init; }
    }

    /// <summary>A library on a Kavita server.</summary>
    /// <param name="Id">Kavita library ID</param>
    /// <param name="Name">Kavita library name</param>
    public sealed record KavitaLibraryInfo(string Id, string Name);
}
