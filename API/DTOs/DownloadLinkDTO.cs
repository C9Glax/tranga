namespace API.DTOs;

/// <summary>
/// DownloadExtension Link Entry
/// </summary>
public sealed record DownloadLinkDTO
{
    /// <summary>
    /// ID of the Manga
    /// </summary>
    public required Guid MangaId { get; init; }
    
    /// <summary>
    /// ID of the DownloadExtension Link Entry
    /// </summary>
    public required Guid DownloadLinkId { get; init; }
    
    /// <summary>
    /// ID of the DownloadExtension
    /// </summary>
    public required Guid DownloadExtensionId { get; init; }
    
    /// <summary>
    /// ID of the Cover-File, if it exists
    /// </summary>
    public Guid? CoverFileId { get; init; }
    
    /// <summary>
    /// Name of the Manga
    /// </summary>
    public string? Title { get; init; }
    
    /// <summary>
    /// Description of the Manga
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Url on the DownloadExtension
    /// </summary>
    public string? Url { get; init; }
    
    /// <summary>
    /// DownloadLink is used as Match for Manga
    /// </summary>
    public bool Matched { get; init; }
}