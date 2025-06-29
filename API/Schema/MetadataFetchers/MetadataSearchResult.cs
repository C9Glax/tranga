namespace API.Schema.MetadataFetchers;

public record MetadataSearchResult(string Identifier, string Name, string Url, string? Description = null, string? CoverUrl = null);