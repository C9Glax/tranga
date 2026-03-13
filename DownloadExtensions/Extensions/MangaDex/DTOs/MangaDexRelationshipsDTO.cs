using System.Text.Json.Nodes;

namespace DownloadExtensions.Extensions.MangaDex.DTOs;

internal sealed class MangaDexRelationshipsDTO : List<MangaDexRelationshipDTO>;

internal sealed record MangaDexRelationshipDTO(Guid Id, string Type, Dictionary<string, JsonNode>? Attributes = null);