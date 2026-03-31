namespace API.DTOs;

/// <summary>
/// The Chapter DTO
/// </summary>
public sealed record ChapterDTO
{
    /// <summary>
    /// The ID of the Chapter
    /// </summary>
    public required Guid ChapterId { get; init; }
    
    /// <summary>
    /// The ID of the related DownloadLink
    /// </summary>
    public required Guid DownloadLinkId { get; init; }
    
    /// <summary>
    /// The ID of the Download Extension
    /// </summary>
    public required Guid DownloadExtensionId { get; init; }
    
    /// <summary>
    /// The ID on the Download Extension
    /// </summary>
    public required string Identifier { get; init; }
    
    /// <summary>
    /// The Volume-number of the Chapter
    /// </summary>
    public string? Volume { get; init; }
    
    /// <summary>
    /// The Chapter-number of the Chapter
    /// </summary>
    public required string Chapter { get; init; }
    
    /// <summary>
    /// The URL on the Download Extension
    /// </summary>
    public string? Url { get; init; }
    
    /// <summary>
    /// Whether Chapter should be downloaded
    /// </summary>
    public required bool Download { get; init; }
    
    /// <summary>
    /// ID of the File (if it exists)
    /// </summary>
    public Guid? FileId { get; set; }
}