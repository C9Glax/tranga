namespace Database.DownloadContext;

public sealed record DownloadExtensionId<T>
{
    public Guid ParentId { get; init; }
    
    public T? Parent { get; init; }
    
    public required Guid ExtensionIdentifier { get; init; }
    
    public required string Identifier { get; init; }
}