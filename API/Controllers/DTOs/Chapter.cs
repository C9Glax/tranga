using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.DTOs;

/// <summary>
/// <see cref="API.Schema.MangaContext.Chapter"/> DTO
/// </summary>
public sealed record Chapter(string Key, string MangaId, int? Volume, string ChapterNumber, string? Title, IEnumerable<MangaConnectorId<Chapter>> MangaConnectorIds, bool Downloaded, string? FileName) : Identifiable(Key)
{
    /// <summary>
    /// Identifier of the Manga this Chapter belongs to
    /// </summary>
    [Required]
    [Description("Identifier of the Manga this Chapter belongs to")]
    public string MangaId { get; init; } = MangaId;
    
    /// <summary>
    /// Volume number
    /// </summary>
    [Required]
    [Description("Volume number")]
    public int? Volume { get; init; } = Volume;
    
    /// <summary>
    /// Chapter number
    /// </summary>
    [Required]
    [Description("Chapter number")]
    public string ChapterNumber { get; init; } = ChapterNumber;
    
    /// <summary>
    /// Title of the Chapter
    /// </summary>
    [Required]
    [Description("Title of the Chapter")]
    public string? Title { get; init; } = Title;
    
    /// <summary>
    /// Whether Chapter is Downloaded (on disk)
    /// </summary>
    [Required]
    [Description("Whether Chapter is Downloaded (on disk)")]
    public bool Downloaded { get; init; } = Downloaded;
    
    /// <summary>
    /// Ids of the Manga on MangaConnectors
    /// </summary>
    [Required]
    [Description("Ids of the Manga on MangaConnectors")]
    public IEnumerable<MangaConnectorId<Chapter>> MangaConnectorIds { get; init; } = MangaConnectorIds;
    
    /// <summary>
    /// Filename of the archive
    /// </summary>
    [Description("Filename of the archive")]
    public string? FileName { get; init; } = FileName;
}