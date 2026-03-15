namespace Database.DownloadContext;

public sealed record DownloadExtensionId
{
    public Guid ParentId { get; init; }
    
    public DbChapter? Parent { get; init; }
    
    public required Guid ExtensionIdentifier { get; init; }
    
    public required string Identifier { get; init; }
}