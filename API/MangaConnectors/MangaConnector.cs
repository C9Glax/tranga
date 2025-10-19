using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using log4net;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace API.MangaConnectors;

[PrimaryKey("Name")]
public abstract class MangaConnector(string name, string[] supportedLanguages, string[] baseUris, string iconUrl)
{
    [NotMapped] internal IDownloadClient downloadClient { get; init; } = null!;
    [NotMapped] protected ILog Log { get; init; } = LogManager.GetLogger(name);
    [StringLength(32)] public string Name { get; init; } = name;
    [StringLength(8)] public string[] SupportedLanguages { get; init; } = supportedLanguages;
    [StringLength(2048)] public string IconUrl { get; init; } = iconUrl;
    [StringLength(256)] public string[] BaseUris { get; init; } = baseUris;
    public bool Enabled { get; internal set; } = true;

    public abstract (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName);
    public abstract (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url);
    public abstract (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite);
    public abstract (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> mangaId, string? language = null);
    internal abstract string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId);

    public bool UrlMatchesConnector(string url) =>
        BaseUris.Any(baseUri => Regex.IsMatch(url, "https?://" + baseUri + "/.*"));

    private static readonly object _coverLock = new();

    internal string? SaveCoverImageToCache(MangaConnectorId<Manga> mangaId, int retries = 3)
    {
        if (retries < 0)
            return null;

        Regex urlRex = new(@"https?:\/\/((?:[a-zA-Z0-9-]+\.)+[a-zA-Z0-9]+)\/(?:.+\/)*(.+\.([a-zA-Z]+))");
        Match match = urlRex.Match(mangaId.Obj.CoverUrl);
        string filename = $"{match.Groups[1].Value}-{mangaId.ObjId}.{mangaId.MangaConnectorName}.{match.Groups[3].Value}";
        string saveImagePath = Path.Join(TrangaSettings.CoverImageCacheOriginal, filename);

        try
        {
            lock (_coverLock)
            {
                if (File.Exists(saveImagePath))
                    return filename;

                HttpResponseMessage coverResult = downloadClient
                    .MakeRequest(mangaId.Obj.CoverUrl, RequestType.MangaCover, $"https://{match.Groups[1].Value}")
                    .Result;

                if ((int)coverResult.StatusCode < 200 || (int)coverResult.StatusCode >= 300)
                    return SaveCoverImageToCache(mangaId, --retries);

                Directory.CreateDirectory(TrangaSettings.CoverImageCacheOriginal);
                byte[] imageBytes;

                using (MemoryStream ms = new())
                {
                    coverResult.Content.ReadAsStream().CopyTo(ms);
                    imageBytes = ms.ToArray();
                }

                File.WriteAllBytes(saveImagePath, imageBytes);

                using (Image image = Image.Load(imageBytes))
                {
                    var encoder = new JpegEncoder { Quality = 40 };

                    try
                    {
                        Directory.CreateDirectory(TrangaSettings.CoverImageCacheLarge);
                        string largePath = Path.Join(TrangaSettings.CoverImageCacheLarge, filename);
                        using (Image large = image.Clone(x => x.Resize(new ResizeOptions { Size = Constants.ImageLgSize, Mode = ResizeMode.Max })))
                            large.SaveAsJpeg(largePath, encoder);
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"Failed to write LARGE cover for {filename}: {e.Message}");
                    }

                    try
                    {
                        Directory.CreateDirectory(TrangaSettings.CoverImageCacheMedium);
                        string mediumPath = Path.Join(TrangaSettings.CoverImageCacheMedium, filename);
                        using (Image medium = image.Clone(x => x.Resize(new ResizeOptions { Size = Constants.ImageMdSize, Mode = ResizeMode.Max })))
                            medium.SaveAsJpeg(mediumPath, encoder);
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"Failed to write MEDIUM cover for {filename}: {e.Message}");
                    }

                    try
                    {
                        Directory.CreateDirectory(TrangaSettings.CoverImageCacheSmall);
                        string smallPath = Path.Join(TrangaSettings.CoverImageCacheSmall, filename);
                        using (Image small = image.Clone(x => x.Resize(new ResizeOptions { Size = Constants.ImageSmSize, Mode = ResizeMode.Max })))
                            small.SaveAsJpeg(smallPath, encoder);
                    }
                    catch (Exception e)
                    {
                        Log.Warn($"Failed to write SMALL cover for {filename}: {e.Message}");
                    }
                }
            }
        }
        catch (Exception e)
        {
            Log.Error($"Error saving cover {filename}: {e}");
        }

        return filename.CleanNameForWindows();
    }

    public async Task<Stream?> DownloadImage(string imageUrl, CancellationToken ct)
    {
        HttpResponseMessage requestResult =
            await downloadClient.MakeRequest(imageUrl, RequestType.MangaImage, cancellationToken: ct);
        return requestResult.IsSuccessStatusCode ? await requestResult.Content.ReadAsStreamAsync(ct) : null;
    }
}
