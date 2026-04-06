using Common.Datatypes;

namespace Extensions.Data;

public sealed record SearchResult
{
    public required Guid MetadataExtensionIdentifier { get; init; }
    
    public required string Identifier { get; init; }
    
    public required MemoryStream Cover { get; init; }
    
    public required string Series { get; init; }
    
    public string? Summary { get; init; }
    
    public int? Year { get; init; }
    
    public string[]? Authors { get; init; }
    
    public string[]? Artists { get; init; }
    
    public string[]? Genres { get; init; }
    
    public string? Url { get; init; }
    
    public Language? Language { get; init; }
    
    public ReleaseStatus? Status { get; init; }
    
    public bool? NSFW { get; init; }
}