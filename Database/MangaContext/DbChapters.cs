using System.ComponentModel.DataAnnotations;

namespace Database.MangaContext;

public sealed record DbChapter
{
    public Guid ChapterId { get; internal set; }
    
    public required Guid MangaId { get; init; }
    
    [StringLength(2048)]
    public string? Title { get; set; }

    [StringLength(16)]
    public string? Volume { get; set; }
    
    [StringLength(16)]
    public required string Number { get; set; }
    
    public DateTimeOffset? ReleaseDate { get; set; }

    #region Navigations
    
    public DbManga? Manga { get; internal set; }
    
    public ICollection<DbChapterDownloadLink>? DownloadLinks { get; internal set; }

    #endregion
}