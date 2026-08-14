using Extensions.Extensions.Suwayomi;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Services.Manga.Features.Suwayomi;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetSuwayomiSourcesEndpoint
{
    /// <summary>
    /// Get the sources of all installed Suwayomi extensions
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>The installed sources, each with the Tranga download-extension id it is registered under</returns>
    /// <response code="200">The installed sources</response>
    /// <response code="503">The Suwayomi sidecar is unreachable</response>
    public static async Task<Results<Ok<SuwayomiSourceInfo[]>, StatusCodeHttpResult>> Handle(CancellationToken ct)
    {
        if (await SuwayomiExtensionManager.GetSourcesAsync(ct) is not { } sources)
            return TypedResults.StatusCode(StatusCodes.Status503ServiceUnavailable);

        return TypedResults.Ok(sources);
    }
}
