using Common.Datatypes;

namespace Database.DownloadContext;

public sealed record DbChapter : IRef
{
    public Guid ChapterId { get; init; }
    
    public bool IsDownloaded { get; set; }
    
    public ICollection<DownloadExtensionId>? DownloadExtensionIds { get; init; }
    
    public Guid MangaId { get; init; }
    
    public DbManga? Manga { get; init; }
    
    public ComicInfo? ComicInfo { get; set; }
}