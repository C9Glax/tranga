namespace API.DTOs;

/// <summary>
/// A Metadata-Extension
/// </summary>
public sealed record MetadataExtensionDTO
{
    /// <summary>
    /// The unique Identifier of this Extension
    /// </summary>
    public required Guid ExtensionIdentifier { get; init; }
    
    /// <summary>
    /// The name of the Extension
    /// </summary>
    public required string Name { get; init; }
    
    /// <summary>
    /// The Url of the Extension
    /// </summary>
    public required string Url { get; init; }
}