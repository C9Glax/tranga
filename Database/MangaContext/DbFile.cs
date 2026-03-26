namespace Database.MangaContext;

public sealed class DbFile
{
    public Guid Id { get; init; }
    
    public required string Name { get; set; }
    
    public required string Path { get; set; }
    
    public required string MimeType { get; init; }
}