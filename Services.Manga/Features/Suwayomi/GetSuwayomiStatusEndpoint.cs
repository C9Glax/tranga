using Extensions.Extensions.Suwayomi;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Services.Manga.Features.Suwayomi;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetSuwayomiStatusEndpoint
{
    /// <summary>
    /// Status of the Suwayomi sidecar
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>Whether the sidecar is enabled and reachable, and what it reports about itself</returns>
    /// <response code="200">Status of the Suwayomi sidecar</response>
    public static async Task<Ok<SuwayomiStatus>> Handle(CancellationToken ct) =>
        TypedResults.Ok(await SuwayomiExtensionManager.GetStatusAsync(ct));
}
