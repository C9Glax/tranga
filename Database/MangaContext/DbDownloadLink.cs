namespace Database.MangaContext;

public sealed class DbDownloadLink
{
    public Guid Id { get; init; }
    
    public required Guid MangaId { get; init; }
    
    public DbManga? Manga { get; init; }
    
    public required Guid DownloadExtensionId { get; init; }
    
    public required string Identifier { get; init; }
    
    public string? Url { get; init; }
    
    public required string Title { get; init; }
    
    public string? Description { get; init; }
    
    public Guid? CoverId { get; init; }
    
    public DbFile? Cover { get; set; }
    
}