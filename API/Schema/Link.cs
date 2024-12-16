using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("LinkId")]
public class Link(string linkProvider, string linkUrl)
{
    [MaxLength(64)]
    public string LinkId { get; init; } = TokenGen.CreateToken(typeof(Link), 64);
    public string LinkProvider { get; init; } = linkProvider;
    public string LinkUrl { get; init; } = linkUrl;

    public override bool Equals(object? obj)
    {
        if (obj is not Link other)
            return false;
        return other.LinkProvider == LinkProvider && other.LinkUrl == LinkUrl;
    }
}