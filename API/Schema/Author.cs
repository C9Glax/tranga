using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("Key")]
public class Author(string authorName) : Identifiable(TokenGen.CreateToken(typeof(Author), authorName))
{
    [StringLength(128)]
    [Required]
    public string AuthorName { get; init; } = authorName;

    public override string ToString() => $"{base.ToString()} {AuthorName}";
}