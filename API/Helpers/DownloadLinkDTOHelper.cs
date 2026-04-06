using API.Entities;
using Database.MangaContext;

namespace API.Helpers;

public static class DownloadLinkDTOHelper
{
    public static DownloadLink ToDTO(this DbMangaDownloadSource source) => new()
    {
        MangaId = source.MangaId,
        DownloadId = source.DownloadSourceId,
        DownloadExtensionId = source.DownloadSource.DownloadId,
        Identifier = source.DownloadSource.Identifier,
        Matched = source.Matched,
        Priority = source.Priority,
        Series = source.DownloadSource.Series,
        Summary = source.DownloadSource.Summary,
        Language = source.DownloadSource.Language,
        Url = source.DownloadSource.Url,
        CoverId = source.DownloadSource.CoverId ?? source.DownloadSource.Cover?.FileId,
        NSFW = source.DownloadSource.NSFW
    };
}