using System.ComponentModel.DataAnnotations;

namespace Database.MangaContext;

public sealed record DbDownloadSource
{
    public Guid DownloadId { get; internal set; }
    
    public required Guid DownloadExtension { get; init; }
    
    public required string Identifier { get; init; }
    
    [StringLength(1024)]
    public required string Series { get; set; }
    
    [StringLength(4096)]
    public string? Summary { get; set; }
    
    [StringLength(8)]
    public string? Language { get; set; }

    public string? Url { get; set; }
    
    public Guid? CoverId { get; set; }

    #region Navigations
    
    public ICollection<DbMangaDownloadSource>? MangaDownloadSources { get; init; }
    
    public DbFile? Cover { get; set; }

    #endregion
}