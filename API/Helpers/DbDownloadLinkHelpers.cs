using API.DTOs;
using Database.MangaContext;

namespace API.Helpers;

internal static class DbDownloadLinkHelpers
{
    public static DownloadLinkDTO ToDTO(this DbDownloadLink link) => new()
    {
        DownloadLinkId = link.Id,
        DownloadExtensionId = link.DownloadExtensionId,
        Url = link.Url
    };
}