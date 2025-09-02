using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.MangaContext;

[PrimaryKey("Key")]
public class Author(string authorName) : Identifiable(TokenGen.CreateToken(typeof(Author), authorName))
{
    [StringLength(128)]
    public string AuthorName { get; init; } = authorName;

    public override string ToString() => $"{base.ToString()} {AuthorName}";
}