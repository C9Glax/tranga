using System.ComponentModel.DataAnnotations;

namespace API.Entities;

public sealed record MangaDownloadLink : DownloadLink
{
    public required Guid MangaId { get; init; }
    
    public required bool Matched { get; init; }
    
    public required int Priority { get; init; }
    
    [StringLength(8)]
    public string? Language { get; set; }
}