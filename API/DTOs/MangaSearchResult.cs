namespace API.DTOs;

public sealed record MangaSearchResult
{
    public required string Title { get; init; }
}