using System.ComponentModel.DataAnnotations;

namespace API.Entities;

public sealed record DownloadLink
{
    public required Guid MangaId { get; init; }
    
    public required Guid DownloadId { get; init; }
    
    public required Guid DownloadExtensionId { get; init; }
    
    public required string Identifier { get; init; }
    
    public required bool Matched { get; init; }
    
    public required int Priority { get; init; }
    
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