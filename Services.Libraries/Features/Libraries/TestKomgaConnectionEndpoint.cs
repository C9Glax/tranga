using Komga.Client.Client;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Services.Libraries.Features.Libraries;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class TestKomgaConnectionEndpoint
{
    /// <summary>
    /// Validates Komga credentials without persisting anything
    /// </summary>
    /// <param name="req">Request parameters</param>
    /// <param name="ct"></param>
    /// <returns>200 OK if the credentials are valid</returns>
    /// <response code="200">Credentials are valid</response>
    /// <response code="400">Neither or both auth modes given</response>
    /// <response code="401">Credentials are invalid</response>
    public static async Task<Results<Ok, BadRequest<string>, UnauthorizedHttpResult>> Handle([FromBody]TestKomgaConnectionRequest req, CancellationToken ct)
    {
        bool hasApiKey = !string.IsNullOrEmpty(req.ApiKey);
        bool hasCredentials = !string.IsNullOrEmpty(req.Username) && !string.IsNullOrEmpty(req.Password);

        if (hasApiKey == hasCredentials)
            return TypedResults.BadRequest("Provide exactly one of ApiKey or (Username and Password).");

        try
        {
            if (hasCredentials)
            {
                await Extensions.Extensions.Komga.MintApiKey(req.BaseUrl, req.Username!, req.Password!, ct);
            }
            else
            {
                Extensions.Extensions.Komga komga = new(req.BaseUrl, req.ApiKey!);
                await komga.GetSeriesList(ct);
            }
            return TypedResults.Ok();
        }
        catch (ApiException)
        {
            return TypedResults.Unauthorized();
        }
        catch (HttpRequestException)
        {
            return TypedResults.Unauthorized();
        }
    }

    public sealed record TestKomgaConnectionRequest
    {
        public required string BaseUrl { get; init; }
        public string? ApiKey { get; init; }
        public string? Username { get; init; }
        public string? Password { get; init; }
    }
}
