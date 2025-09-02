using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.DTOs;

/// <summary>
/// <see cref="API.Schema.MangaContext.AltTitle"/> DTO
/// </summary>
public sealed record AltTitle(string Language, string Title)
{
    /// <summary>
    /// Language of the Title
    /// </summary>
    [Required]
    [Description("Language of the Title")]
    public string Language { get; init; } = Language;
    
    /// <summary>
    /// Title
    /// </summary>
    [Required]
    [Description("Title")]
    public string Title { get; init; } = Title;
}