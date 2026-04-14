namespace Services.Manga.Database;

public sealed record DbFile
{
    public Guid FileId { get; init; }
    
    public required string Path { get; init; }
    
    public required string Name { get; init; }
    
    public required string MimeType { get; init; }
}