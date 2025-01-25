using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("AltTitleId")]
public class MangaAltTitle(string language, string title)
{
    [MaxLength(64)]
    public string AltTitleId { get; init; } = TokenGen.CreateToken("AltTitle", string.Join(language, title));
    [MaxLength(8)]
    public string Language { get; init; } = language;
    public string Title { get; set; } = title;
}