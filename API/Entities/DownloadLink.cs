using System.ComponentModel.DataAnnotations;

namespace API.Entities;

public record DownloadLink
{
    public required Guid DownloadId { get; init; }
    
    public required Guid DownloadExtensionId { get; init; }
    
    public required string Identifier { get; init; }
    
    [StringLength(1024)]
    public required string Series { get; set; }
    
    [StringLength(4096)]
    public required string? Summary { get; set; }
    
    [StringLength(8)]
    public string? Language { get; set; }
    
    public required string? Url { get; init; }
    
    public required Guid? CoverId { get; set; }
    
    public required bool? NSFW { get; init; }
}