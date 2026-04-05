using System.ComponentModel.DataAnnotations;
using Common.Datatypes;

namespace Database.MangaContext;

public sealed record DbMetadataSource
{
    public Guid MetadataId { get; internal set; }
    
    public required Guid MetadataExtension { get; init; }
    
    public required string Identifier { get; init; }
    
    [StringLength(1024)]
    public required string Series { get; set; }
    
    [StringLength(4096)]
    public string? Summary { get; set; }
    
    public int? Year { get; set; }
    
    [StringLength(8)]
    public string? Language { get; set; }
    
    public int? ChaptersNumber { get; set; }
    
    public ReleaseStatus? Status { get; init; }
    
    public Guid? CoverId { get; set; }
    
    public string? Url { get; set; }

    #region Navigations
    
    public DbFile? Cover { get; set; }
    
    public ICollection<DbGenre>? Genres { get; set; }
    
    public ICollection<DbPerson>? Artists { get; set; }
    
    public ICollection<DbPerson>? Authors { get; set; }
    
    public ICollection<DbMangaMetadataSource>? MangaMetadataSources { get; init; }

    #endregion
}