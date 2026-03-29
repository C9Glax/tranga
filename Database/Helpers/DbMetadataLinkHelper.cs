using Common.Helpers;
using Database.MangaContext;
using Settings;

namespace Database.Helpers;

public static class DbMetadataLinkHelper
{
    public static async Task SaveCover(this DbMetadataLink metadataLink, MangaContext.MangaContext mangaContext, MemoryStream memoryStream, CancellationToken ct)
    {
        try
        {
            await memoryStream.ToJpeg(ct);
            DbFile file = new ()
            {
                Name = $"{metadataLink.Id}.jpg",
                Path = Constants.CoverDirectory,
                MimeType = "image/jpeg"
            };
            await mangaContext.AddAsync(file, ct);
            await file.SaveFile(memoryStream, ct);
            metadataLink.Cover = file;
            metadataLink.CoverId = file.Id;
        }
        catch
        {
            DbFile file = new ()
            {
                Name = $"{metadataLink.Id}",
                Path = Constants.CoverDirectory,
                MimeType = "image/png"
            };
            await mangaContext.AddAsync(file, ct);
            await file.SaveFile(memoryStream, ct);
            metadataLink.Cover = file;
            metadataLink.CoverId = file.Id;
        }
    }
}