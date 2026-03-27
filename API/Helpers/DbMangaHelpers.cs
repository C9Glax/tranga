using API.DTOs;
using Database.MangaContext;

namespace API.Helpers;

internal static class DbMangaHelpers
{
    public static MangaDTO ToDTO(this DbManga manga) => new()
    {
        MangaId = manga.Id,
        Title = manga.Series,
        Monitored = manga.Monitor,
        DownloadLinks = manga.DownloadLinks?.Select(d => d.ToDTO()).ToArray(),
        MetadataLinks = manga.MetadataLinks?.Select(l => l.ToDTO()).ToArray(),
    };
}