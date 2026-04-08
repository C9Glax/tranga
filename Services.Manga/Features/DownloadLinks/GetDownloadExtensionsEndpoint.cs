using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Entities.DownloadExtensions;

namespace Services.Manga.Features.DownloadLinks;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
public abstract class GetDownloadExtensionsEndpoint
{
    /// <summary>
    /// Get Download-Extensions
    /// </summary>
    /// <returns>A List of Download-Extensions</returns>
    /// <response code="200">A List of Download-Extensions</response>
    public static Ok<DownloadExtensionsList> Handle() => TypedResults.Ok(new DownloadExtensionsList());
}