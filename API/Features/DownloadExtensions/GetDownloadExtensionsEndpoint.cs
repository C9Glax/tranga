using API.DTOs;
using DownloadExtensions;
using MetadataExtensions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace API.Features.DownloadExtensions;

/// <summary>
/// Get all Download-Extensions
/// </summary>
public abstract class GetDownloadExtensionsEndpoint
{
    /// <summary>
    /// Get all Download-Extensions
    /// </summary>
    /// <param name="ct"></param>
    /// <returns>A list of all Download-Extension</returns>
    /// <response code="200">A list of all Download-Extension</response>
    public static Ok<DownloadExtensionDTO[]> Handle(CancellationToken ct)
    {
        DownloadExtensionDTO[] ret = DownloadExtensionsCollection.Extensions.Select(e => new DownloadExtensionDTO()
        {
            ExtensionIdentifier = e.Identifier,
            Name = e.Name,
            Url = e.BaseUrl
        }).ToArray();
        return TypedResults.Ok(ret);
    }
}