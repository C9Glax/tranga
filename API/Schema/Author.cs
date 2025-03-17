using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("AuthorId")]
public class Author(string authorName)
{
    [StringLength(64)]
    [Required]
    public string AuthorId { get; init; } = TokenGen.CreateToken(typeof(Author), authorName);
    [StringLength(128)]
    [Required]
    public string AuthorName { get; init; } = authorName;
}