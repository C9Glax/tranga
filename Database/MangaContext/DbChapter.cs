using Data;

namespace Database.MangaContext;

public sealed record DbChapter : IRef
{
    public Guid ChapterId { get; init; }
    
    public ICollection<DownloadExtensionId<DbChapter>>? DownloadExtensionIds { get; init; }
    
    public Guid MangaId { get; init; }
    
    public DbManga? Manga { get; init; }
    
    public ComicInfo? ComicInfo { get; set; }
}