namespace API.Entities;

public sealed record MatchResult : Manga
{
    public required Guid DownloadExtensionId { get; init; }
    
    public required string Identifier { get; init; }
    
    public required string? Url { get; init; }
}