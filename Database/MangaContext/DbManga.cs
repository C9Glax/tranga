using System.ComponentModel.DataAnnotations;

namespace Database.MangaContext;

public sealed record DbManga
{
    public Guid MangaId { get; internal set; }

    #region Navigations
    
    public ICollection<DbChapter>? Chapters { get; internal set; }
    
    public ICollection<DbMetadataSource>? MetadataSources { get; internal set; }
    
    public ICollection<DbMangaDownloadSources>? DownloadSources { get; internal set; }

    #endregion
}