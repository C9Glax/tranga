using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("AuthorId")]
public class Author(string authorName)
{
    [MaxLength(64)]
    public string AuthorId { get; init; } = TokenGen.CreateToken(typeof(Author), 64);
    public string AuthorName { get; init; } = authorName;

    public override bool Equals(object? obj)
    {
        if (obj is not Author other)
            return false;
        return other.AuthorName == AuthorName;
    }
}