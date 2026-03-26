namespace Database.MangaContext;

public sealed class DbDownloadLink
{
    public Guid Id { get; init; }
    
    public required Guid MangaId { get; init; }
    
    public DbManga? Manga { get; init; }
    
    public required Guid DownloadExtensionId { get; init; }
    
    public required string Identifier { get; init; }
    
    public string? Url { get; init; }
}