namespace Database.MangaContext;

public sealed record DbChapter : IRef
{
    public Guid ChapterId { get; init; }
    
    public ICollection<ExtensionId<DbChapter>>? ExtensionIds { get; init; }
    
    public Guid MangaId { get; init; }
    
    public DbManga? Manga { get; init; }
    
    public string? Title { get; init; }
    
    public string? Number { get; init; }
    
    public string? Volume { get; init; }
}