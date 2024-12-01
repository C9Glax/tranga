using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("Name")]
public class MangaConnector(string name, string[] supportedLanguages, string[] baseUris)
{
    [MaxLength(32)]
    public string Name { get; init; } = name;
    public string[] SupportedLanguages { get; init; } = supportedLanguages;
    public string[] BaseUris { get; init; } = baseUris;

    [ForeignKey("MangaIds")]
    public virtual Manga[] Mangas { get; internal set; } = [];
}