using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using API.Schema.MangaContext;

namespace API.Controllers.DTOs;

/// <summary>
/// <see cref="Schema.MangaContext.Manga"/> DTO
/// </summary>
public record Manga(string Key, string Name, string Description, MangaReleaseStatus ReleaseStatus, IEnumerable<MangaConnectorId> MangaConnectorIds, float IgnoreChaptersBefore, uint? Year, string? OriginalLanguage, IEnumerable<string> ChapterIds, IEnumerable<Author> Authors, IEnumerable<string> Tags, IEnumerable<Link> Links, IEnumerable<AltTitle> AltTitles)
    : MinimalManga(Key, Name, Description, ReleaseStatus, MangaConnectorIds)
{
    /// <summary>
    /// Chapter cutoff for Downloads (Chapters before this will not be downloaded)
    /// </summary>
    [Required]
    [Description("Chapter cutoff for Downloads (Chapters before this will not be downloaded)")]
    public float IgnoreChaptersBefore { get; init; } = IgnoreChaptersBefore;
    
    /// <summary>
    /// Release Year
    /// </summary>
    [Description("Release Year")]
    public uint? Year { get; init; } = Year;
    
    /// <summary>
    /// Release Language
    /// </summary>
    [Description("Release Language")]
    public string? OriginalLanguage { get; init; } = OriginalLanguage;
    
    /// <summary>
    /// Keys of ChapterDTOs
    /// </summary>
    [Required]
    [Description("Keys of ChapterDTOs")]
    public IEnumerable<string> ChapterIds { get; init; } = ChapterIds;
    
    /// <summary>
    /// Author-names
    /// </summary>
    [Required]
    [Description("Author-names")]
    public IEnumerable<Author> Authors { get; init; } = Authors;
    
    /// <summary>
    /// Manga Tags
    /// </summary>
    [Required]
    [Description("Manga Tags")]
    public IEnumerable<string> Tags { get; init; } = Tags;
    
    /// <summary>
    /// Links for more Metadata
    /// </summary>
    [Required]
    [Description("Links for more Metadata")]
    public IEnumerable<Link> Links { get; init; } = Links;
    
    /// <summary>
    /// Alt Titles of Manga
    /// </summary>
    [Required]
    [Description("Alt Titles of Manga")]
    public IEnumerable<AltTitle> AltTitles { get; init; } = AltTitles;
}