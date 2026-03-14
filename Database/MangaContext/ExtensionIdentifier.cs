namespace Database.MangaContext;

public sealed record DownloadExtensionId<T> where T : IRef
{
    public Guid ParentId { get; init; }
    
    public T? Parent { get; init; }
    
    public required Guid ExtensionIdentifier { get; init; }
    
    public required string Identifier { get; init; }
}