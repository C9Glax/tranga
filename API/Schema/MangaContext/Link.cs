using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace API.Schema.MangaContext;

[PrimaryKey("Key")]
public class Link(string linkProvider, string linkUrl) : Identifiable(TokenGen.CreateToken(typeof(Link), linkProvider, linkUrl))
{
    [StringLength(64)]
    public string LinkProvider { get; init; } = linkProvider;
    [StringLength(2048)]
    [Url]
    public string LinkUrl { get; init; } = linkUrl;

    public override string ToString() => $"{base.ToString()} {LinkProvider} {LinkUrl}";
}