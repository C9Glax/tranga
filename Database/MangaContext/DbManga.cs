using System.ComponentModel.DataAnnotations;

namespace Database.MangaContext;

public sealed record DbManga
{
    public Guid MangaId { get; internal set; }

    public required bool Monitored { get; set; } = false;

    #region Navigations
    
    public ICollection<DbChapter>? Chapters { get; internal set; }
    
    public ICollection<DbMetadataSource>? MetadataSources { get; internal set; }
    
    public ICollection<DbMangaDownloadSources>? DownloadSources { get; internal set; }

    #endregion
}