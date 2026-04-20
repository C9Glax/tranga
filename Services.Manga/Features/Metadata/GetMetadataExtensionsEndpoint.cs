using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Entities.MetadataExtensions;

namespace Services.Manga.Features.Metadata;

/// <summary>
/// <inheritdoc cref="Handle"/>
/// </summary>
internal abstract class GetMetadataExtensionsEndpoint
{
    /// <summary>
    /// Get Metadata-Extensions
    /// </summary>
    /// <returns>A List of Metadata-Extensions</returns>
    /// <response code="200">A List of Metadata-Extensions</response>
    public static Ok<MetadataExtensionsList> Handle() => TypedResults.Ok(new MetadataExtensionsList());
}