using System.ComponentModel.DataAnnotations;

namespace Database.MangaContext;

public sealed record DbManga
{
    public Guid MangaId { get; init; }

    public required bool Monitored { get; set; } = false;

    #region Navigations
    
    public ICollection<DbChapter>? Chapters { get; init; }

    public ICollection<DbMangaMetadataSource>? MetadataSources { get; init; }
    
    public ICollection<DbMangaDownloadSource>? DownloadSources { get; init; }

    #endregion
}