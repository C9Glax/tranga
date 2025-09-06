using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace API.Controllers.DTOs;

/// <summary>
/// <see cref="API.Schema.Identifiable"/>
/// </summary>
public record Identifiable(string Key)
{
    /// <summary>
    /// Unique Identifier of the DTO
    /// </summary>
    [Required]
    [Description("Unique Identifier of the DTO")]
    [StringLength(TokenGen.MaximumLength, MinimumLength = TokenGen.MinimumLength)]
    public string Key { get; init; } = Key;
}