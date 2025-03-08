using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("Tag")]
public class MangaTag(string tag)
{
    public string Tag { get; init; } = tag;
}