// ReSharper disable ClassNeverInstantiated.Global
namespace Extensions.Extensions.MangaDex.DTOs;

internal sealed record MangaDexChapterResultDTO(string Result, string Response, MangaDexChapterDTO[] Data, int Limit, int Offset, int Total)
    : MangaDexCollectionResponse<MangaDexChapterDTO>(Result, Response, Data, Limit, Offset, Total);

internal sealed record MangaDexChapterDTO(Guid Id, MangaDexChapterAttributesDTO Attributes, MangaDexRelationshipsDTO Relationships);

internal sealed record MangaDexChapterAttributesDTO(string? Title, string? Volume, string Chapter, string? ExternalUrl);