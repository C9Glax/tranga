using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("AltTitleId")]
public class MangaAltTitle(string language, string title)
{
    [StringLength(64)]
    [Required]
    public string AltTitleId { get; init; } = TokenGen.CreateToken("AltTitle", language, title);
    [StringLength(8)]
    [Required]
    public string Language { get; init; } = language;
    [StringLength(256)]
    [Required]
    public string Title { get; set; } = title;
}