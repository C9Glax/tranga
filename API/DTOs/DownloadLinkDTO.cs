namespace API.DTOs;

/// <summary>
/// DownloadExtension Link Entry
/// </summary>
public sealed record DownloadLinkDTO
{
    /// <summary>
    /// ID of the DownloadExtension Link Entry
    /// </summary>
    public required Guid DownloadLinkId { get; init; }
    
    /// <summary>
    /// ID of the DownloadExtension
    /// </summary>
    public required Guid DownloadExtensionId { get; init; }

    /// <summary>
    /// Url on the DownloadExtension
    /// </summary>
    public string? Url { get; init; }
}