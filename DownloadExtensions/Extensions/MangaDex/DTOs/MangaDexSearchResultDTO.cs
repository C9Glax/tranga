using System.Text.Json.Nodes;
// ReSharper disable ClassNeverInstantiated.Global

namespace DownloadExtensions.Extensions.MangaDex.DTOs;

internal sealed record MangaDexSearchResultDTO(string Result, string Response, MangaDexMangaDTO[] Data, int Limit, int Offset, int Total)
    : MangaDexCollectionResponse<MangaDexMangaDTO>(Result, Response, Data, Limit, Offset, Total);

internal sealed record MangaDexMangaDTO(Guid Id, MangaDexMangaAttributesDTO Attributes, MangaDexRelationshipsDTO Relationships);

internal sealed class MangaDexMangaAttributesDTO : Dictionary<string, JsonObject>
{
    public T? GetAttribute<T>(string attribute)
    {
        if (!this.TryGetValue(attribute, out JsonObject? jsonObject) ||
            jsonObject.FirstOrDefault().Value is not { } node ||
            node.GetValue<T>() is not { } value)
            return default;
        return value;
    }
}