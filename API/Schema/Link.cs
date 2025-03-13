using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("LinkId")]
public class Link(string linkProvider, string linkUrl)
{
    [StringLength(64)]
    [Required]
    public string LinkId { get; init; } = TokenGen.CreateToken(typeof(Link), linkProvider, linkUrl);
    [StringLength(64)]
    [Required]
    public string LinkProvider { get; init; } = linkProvider;
    [StringLength(2048)]
    [Required]
    [Url]
    public string LinkUrl { get; init; } = linkUrl;

    public override bool Equals(object? obj)
    {
        if (obj is not Link other)
            return false;
        return other.LinkProvider == LinkProvider && other.LinkUrl == LinkUrl;
    }
}