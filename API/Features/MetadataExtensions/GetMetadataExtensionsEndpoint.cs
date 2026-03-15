using API.DTOs;
using MetadataExtensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API.Features.MetadataExtensions;

/// <summary>
/// Get all Metadata-Extensions
/// </summary>
public abstract class GetMetadataExtensionsEndpoint
{

    /// <summary>
    /// Get all Metadata-Extensions
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>A list of all Metadata-Extension</returns>
    /// <response code="200">A list of all Metadata-Extension</response>
    public static Ok<MetadataExtensionDTO[]> Handle(CancellationToken ct)
    {
        MetadataExtensionDTO[] ret = MetadataExtensionsCollection.Extensions.Select(e => new MetadataExtensionDTO()
        {
            ExtensionIdentifier = e.Identifier,
            Name = e.Name,
            Url = e.BaseUrl
        }).ToArray();
        return TypedResults.Ok(ret);
    }
}