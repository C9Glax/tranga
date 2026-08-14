using Extensions.Extensions.Suwayomi;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Services.Manga.Features.Suwayomi;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetSuwayomiIconEndpoint
{
    /// <summary>
    /// Get the icon of a Suwayomi extension
    /// </summary>
    /// <param name="iconId">Package name of the extension, as it appears at the end of the sidecar's icon url</param>
    /// <param name="ct"></param>
    /// <returns>The icon image</returns>
    /// <remarks>
    /// The sidecar is not reachable from the browser, so its icons are served back through here. This is the one
    /// Suwayomi endpoint that stays anonymous: it returns nothing but an extension's public logo, and requiring a
    /// token would mean every <c>&lt;img&gt;</c> in the app had to fetch through the authenticated client instead.
    /// The identifier is validated as a package name before use, so it cannot be pointed at other sidecar paths.
    /// </remarks>
    /// <response code="200">The icon image</response>
    /// <response code="404">No such icon, or the Suwayomi sidecar is unreachable</response>
    public static async Task<Results<FileContentHttpResult, NotFound>> Handle([FromRoute]string iconId, CancellationToken ct)
    {
        if (await SuwayomiExtensionManager.GetIconAsync(iconId, ct) is not { } icon)
            return TypedResults.NotFound();

        return TypedResults.File(icon.Content, icon.ContentType);
    }
}
