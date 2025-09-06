using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.DTOs;

/// <summary>
/// The <see cref="API.Schema.MangaContext.Author"/> DTO
/// </summary>
public sealed record Author(string Key, string Name) : Identifiable(Key)
{
    /// <summary>
    /// Name of the Author.
    /// </summary>
    [Required]
    [Description("Name of the Author.")]
    public string Name { get; init; } = Name;
}