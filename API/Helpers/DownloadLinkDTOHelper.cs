using API.Entities;
using Database.MangaContext;

namespace API.Helpers;

internal static class DownloadLinkDTOHelper
{
    public static MangaDownloadLink ToDTO(this DbMangaDownloadLinks links) => new()
    {
        MangaId = links.MangaId,
        DownloadId = links.DownloadLinkId,
        DownloadExtensionId = links.DownloadLink.DownloadExtension,
        Identifier = links.DownloadLink.Identifier,
        Matched = links.Matched,
        Priority = links.Priority,
        Series = links.DownloadLink.Series,
        Summary = links.DownloadLink.Summary,
        Language = links.DownloadLink.Language,
        Url = links.DownloadLink.Url,
        CoverId = links.DownloadLink.CoverId ?? links.DownloadLink.Cover?.FileId,
        NSFW = links.DownloadLink.NSFW
    };
    
    public static DownloadLink ToDTO(this DbDownloadLink link) => new()
    {
        DownloadId = link.DownloadLinkId,
        DownloadExtensionId = link.DownloadExtension,
        Identifier = link.Identifier,
        Series = link.Series,
        Summary = link.Summary,
        Language = link.Language,
        Url = link.Url,
        CoverId = link.CoverId ?? link.Cover?.FileId,
        NSFW = link.NSFW
    };
}