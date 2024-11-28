using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;

namespace API.Schema;

[PrimaryKey("MangaConnectorId")]
public class MangaConnector(string name, string[] supportedLanguages, string[] baseUris)
{
    [MaxLength(64)]
    public string MangaConnectorId { get; init; } = TokenGen.CreateToken(typeof(MangaConnector), 64);
    [MaxLength(32)]
    public string Name { get; init; } = name;
    public string[] SupportedLanguages { get; init; } = supportedLanguages;
    public string[] BaseUris { get; init; } = baseUris;
}