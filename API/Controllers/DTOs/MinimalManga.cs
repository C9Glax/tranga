using System.ComponentModel.DataAnnotations;
using API.Schema.MangaContext;
using Newtonsoft.Json;

namespace API.Controllers.DTOs;

public sealed record MinimalManga(string Key, string Name, string Description, MangaReleaseStatus ReleaseStatus, IEnumerable<MangaConnectorId<Manga>>? MangaConnectorIds = null)
{
    [Required] [StringLength(TokenGen.MaximumLength, MinimumLength = TokenGen.MinimumLength)]
    public string Key { get; init; } = Key;
    [Required]
    [JsonRequired]
    public string Name { get; init; } = Name;
    [Required]
    [JsonRequired]
    public string Description { get; init; } = Description;
    [Required]
    [JsonRequired]
    public MangaReleaseStatus ReleaseStatus { get; init; } = ReleaseStatus;
    public IEnumerable<MangaConnectorId<Manga>>? MangaConnectorIds { get; init; } = MangaConnectorIds;
}