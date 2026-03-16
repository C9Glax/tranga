using Common.Datatypes;

namespace Database.DownloadContext;

public sealed record DbChapter
{
    public Guid ChapterId { get; init; }
    
    public bool IsDownloaded { get; set; }
    
    public ICollection<DownloadExtensionId<DbChapter>>? DownloadExtensionIds { get; init; }
    
    public Guid MangaId { get; init; }
    
    public DbManga? Manga { get; init; }
}