using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.DTOs;


/// <summary>
/// <see cref="API.Schema.MangaContext.Link"/> DTO
/// </summary>
public sealed record Link(string Key, string Provider, string Url) : Identifiable(Key)
{
    /// <summary>
    /// Name of the Provider
    /// </summary>
    [Required]
    [Description("Name of the Provider")]
    public string Provider { get; init; } = Provider;
    
    /// <summary>
    /// Url
    /// </summary>
    [Required]
    [Description("Url")]
    public string Url { get; init; } = Url;
}