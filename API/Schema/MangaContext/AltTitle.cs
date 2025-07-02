using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.MangaContext;

[PrimaryKey("Key")]
public class AltTitle(string language, string title) : Identifiable(TokenGen.CreateToken("AltTitle"))
{
    [StringLength(8)]
    [Required]
    public string Language { get; init; } = language;
    [StringLength(256)]
    [Required]
    public string Title { get; init; } = title;

    public override string ToString() => $"{base.ToString()} {Language} {Title}";
}