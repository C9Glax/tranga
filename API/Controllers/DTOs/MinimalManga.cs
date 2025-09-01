using API.Schema.MangaContext;

namespace API.Controllers.DTOs;

public sealed record MinimalManga(string Key, string Name, string Description, MangaReleaseStatus ReleaseStatus);