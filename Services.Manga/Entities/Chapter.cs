using System.ComponentModel.DataAnnotations;

namespace Services.Manga.Entities;

public sealed record Chapter
{
    public required Guid ChapterId { get; init; }
    
    public required Guid MangaId { get; init; }
    
    [StringLength(2048)]
    public required string? Title { get; set; }

    [StringLength(16)]
    public required string? Volume { get; set; }
    
    [StringLength(16)]
    public required string Number { get; set; }
    
    public required DateTimeOffset? ReleaseDate { get; set; }
}