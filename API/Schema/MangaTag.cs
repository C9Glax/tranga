using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("Tag")]
public class MangaTag(string tag)
{
    public string Tag { get; init; } = tag;
    
    [ForeignKey("MangaIds")]
    public virtual Manga[] Mangas { get; internal set; } = [];
}