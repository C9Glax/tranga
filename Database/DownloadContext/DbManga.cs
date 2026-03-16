namespace Database.DownloadContext;

public sealed record DbManga
{
    public Guid MangaId { get; init; }
    
    public bool Download { get; set; }
    
    public ICollection<DbChapter>? Chapters { get; init; }
}