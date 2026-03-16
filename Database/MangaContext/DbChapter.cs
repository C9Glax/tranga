namespace Database.MangaContext;

public sealed record DbChapter
{
    public Guid ChapterId { get; init; }
    
    public ICollection<DownloadExtensionId<DbChapter>>? DownloadExtensionIds { get; init; }
    
    public Guid MangaId { get; init; }
    
    public DbManga? Manga { get; init; }
    
    public required string? Volume { get; init; }
    public required string Chapter { get; init; }
    public required string? Title { get; set; }
    public required string? Description { get; set; }
}