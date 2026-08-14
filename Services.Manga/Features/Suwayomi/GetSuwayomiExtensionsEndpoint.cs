using Extensions.Extensions.Suwayomi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Services.Manga.Features.Suwayomi;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetSuwayomiExtensionsEndpoint
{
    /// <summary>
    /// Get the Suwayomi extension catalogue
    /// </summary>
    /// <param name="refresh">Re-read the configured extension stores first. Hits the network and is slow, so only pass this on an explicit user refresh.</param>
    /// <param name="ct"></param>
    /// <returns>Every extension offered by the configured stores, installed or not</returns>
    /// <response code="200">The extension catalogue</response>
    /// <response code="503">The Suwayomi sidecar is unreachable</response>
    public static async Task<Results<Ok<SuwayomiExtensionInfo[]>, StatusCodeHttpResult>> Handle([FromQuery]bool? refresh, CancellationToken ct)
    {
        if (await SuwayomiExtensionManager.GetExtensionsAsync(refresh ?? false, ct) is not { } extensions)
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);

        return TypedResults.Ok(extensions);
    }
}
