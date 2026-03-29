using Common.Helpers;
using Database.MangaContext;
using Settings;

namespace Database.Helpers;

public static class DbDownloadLinkHelper
{
    public static async Task SaveCover(this DbDownloadLink downloadLink, MangaContext.MangaContext mangaContext, MemoryStream memoryStream, CancellationToken ct)
    {
        try
        {
            await memoryStream.ToJpeg(ct);
            DbFile file = new ()
            {
                Name = $"{downloadLink.Id}.jpg",
                Path = Constants.CoverDirectory,
                MimeType = "image/jpeg"
            };
            await mangaContext.AddAsync(file, ct);
            await file.SaveFile(memoryStream, ct);
            downloadLink.Cover = file;
            downloadLink.CoverId = file.Id;
        }
        catch
        {
            DbFile file = new ()
            {
                Name = $"{downloadLink.Id}",
                Path = Constants.CoverDirectory,
                MimeType = "image/png"
            };
            await mangaContext.AddAsync(file, ct);
            await file.SaveFile(memoryStream, ct);
            downloadLink.Cover = file;
            downloadLink.CoverId = file.Id;
        }
    }
}