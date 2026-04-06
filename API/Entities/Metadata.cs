using System.ComponentModel.DataAnnotations;
using Common.Datatypes;

namespace API.Entities;

public record Metadata
{
    public required Guid MetadataId { get; init; }
    
    public required Guid MetadataExtensionId { get; init; }
    
    public required string Identifier { get; init; }
    
    public bool? Chosen { get; init; }
    
    [StringLength(1024)]
    public required string Series { get; set; }
    
    [StringLength(4096)]
    public required string? Summary { get; set; }
    
    public int? Year { get; set; }
    
    [StringLength(8)]
    public string? Language { get; set; }
    
    public int? ChaptersNumber { get; set; }
    
    public required Guid? CoverId { get; set; }
    
    public string[] Genres { get; init; }
    
    public string[] Authors { get; init; }
    
    public string[] Artists { get; init; }
    
    public required string? Url { get; init; }
    
    public ReleaseStatus? Status { get; init; }
    
    public required bool? NSFW { get; init; }
}