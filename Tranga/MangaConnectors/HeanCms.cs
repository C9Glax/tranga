using System.Diagnostics;
using System.Net;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Tranga.Jobs;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tranga.MangaConnectors;

public abstract class HeanCms : MangaConnector
{
    protected abstract string hostname { get; } // "https://api.example.heancms.com"
    protected virtual string mangaUrlPrefix { get; } = "/series/";

    public HeanCms(GlobalBase clone, string label, string[] supportedLanguages) : base(clone, label, supportedLanguages)
    {
        this.downloadClient = new HttpDownloadClient(clone);
    }

    public override Manga[] GetManga(String publicationTitle = "")
    {
        Log($"GetManga: {publicationTitle}");
        try {
            return FetchMangasByTitle(publicationTitle);
        } catch (Exception e) {
            Log($"GetManga: {publicationTitle}: exception: {e}");
            return Array.Empty<Manga>();
        }
    }

    public override Manga? GetMangaFromId(string seriesSlug)
    {
        Log($"GetMangaFromId: {seriesSlug}");
        try {
            return FetchMangaBySeriesSlug(seriesSlug);
        } catch (Exception e) {
            Log($"GetMangaFromId: {seriesSlug}: exception: {e}");
            return null;
        }
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        Log($"GetMangaFromUrl: {url}");

        int startIndex = url.IndexOf(this.mangaUrlPrefix) + this.mangaUrlPrefix.Length;
        string seriesSlug = url.Substring(startIndex);

        Log($"GetMangaFromUrl: {url}: seriesSlug: {seriesSlug}");

        return GetMangaFromId(seriesSlug);
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        Log($"GetChapters: {manga.publicationId}");
        try {
            return FetchChapters(manga);
        } catch (Exception e) {
            Log($"GetChapters: {manga.publicationId}: exception: {e}");
            return Array.Empty<Chapter>();
        }
    }

    public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
    {
        string seriesSlug = chapter.parentManga.publicationId;
        string? chapterSlug = chapter.id;

        Log($"DownloadChapter: {seriesSlug}: {chapterSlug}");

        if (chapterSlug is null)
        {
            Log($"DownloadChapter: {seriesSlug}: {chapterSlug}: null chapterSlug");
            Log($"DownloadChapter: {seriesSlug}: chapter: {chapter}");
            // IMO this should return HttpStatusCode.BadRequest, but that seems to cause the job to hang
            return HttpStatusCode.InternalServerError;
        }

        try {
            return FetchChapterImages(chapter, progressToken);
        } catch (Exception e) {
            Log($"DownloadChapter: {seriesSlug}: {chapterSlug}: exception: {e}");
            return HttpStatusCode.InternalServerError;
        }
    }

    private readonly string queryPath = $"/query?query_string=";
    private readonly string seriesPath = $"/series";
    private readonly string chapterPath = $"/chapter";

    private string GetQueryUrl(string publicationTitle) => $"{hostname}{queryPath}{publicationTitle}";
    private string GetSeriesSlugUrl(string publicationSlug) => $"{hostname}{seriesPath}/{publicationSlug}";
    private string GetSeriesQueryIdUrl(string seriesId) => $"{hostname}{chapterPath}/query?series_id={seriesId}&perPage=9999&page=1";
    private string GetPublicationChapterSlugUrl(string publicationSlug, string chapterSlug) => $"{hostname}{chapterPath}/{publicationSlug}/{chapterSlug}";

    private Manga[] FetchMangasByTitle(string publicationTitle = "")
    {
        Log($"FetchMangasByTitle: '{publicationTitle}'");

        Manga[]? searchResults = SearchMangasByTitle(publicationTitle);

        if (searchResults is null)
        {
            Log($"FetchMangasByTitle: '{publicationTitle}': null searchResults");
            return Array.Empty<Manga>();
        }

        Log($"FetchMangasByTitle: '{publicationTitle}': searchResults.length: {searchResults.Length}");

        return searchResults;
    }

    private Manga? FetchMangaBySeriesSlug(string seriesSlug)
    {
        Log($"FetchMangaBySeriesSlug: {seriesSlug}");

        JsonObject? result = FetchSeriesBySlug(seriesSlug);

        if (result is null)
        {
            Log($"FetchMangaBySeriesSlug: {seriesSlug}: null result");
            return null;
        }

        Manga retManga = ConvertSeriesToManga(result);

        return retManga;
    }

    private Manga[]? SearchMangasByTitle(string searchQuery)
    {
        Log($"SearchMangasByTitle: '{searchQuery}'");

        string queryUrl = GetQueryUrl(searchQuery);

        Log($"SearchMangasByTitle: '{searchQuery}': queryUrl: {queryUrl}");

        RequestResult requestResult = downloadClient.MakeRequest(queryUrl, RequestType.MangaInfo);

        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            Log($"SearchMangasByTitle: '{searchQuery}': requestResult.statusCode: {requestResult.statusCode}");
            return null;
        }

        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);

        if (result is null)
        {
            Log($"SearchMangasByTitle: '{searchQuery}': null result");
            return null;
        }

        JsonArray series = result["data"]!.AsArray();

        if (series is null)
        {
            Log($"SearchMangasByTitle: '{searchQuery}': null series");
            return null;
        }

        List<Manga> retManga = FetchMangasFromJsonArray(series);

        return retManga.ToArray();
    }

    private JsonObject? FetchSeriesBySlug(string publicationSlug)
    {
        Log($"FetchSeriesBySlug: {publicationSlug}");

        string seriesUrl = GetSeriesSlugUrl(publicationSlug);

        Log($"FetchSeriesBySlug: {publicationSlug}: seriesUrl: {seriesUrl}");

        RequestResult requestResult = downloadClient.MakeRequest(seriesUrl, RequestType.MangaInfo);

        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            Log($"FetchSeriesBySlug: {publicationSlug}: requestResult.statusCode: {requestResult.statusCode}");
            return null;
        }

        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);

        return result;
    }

    private JsonObject? FetchSeriesById(string seriesId)
    {
        Log($"FetchSeriesById: {seriesId}");

        string seriesUrl = GetSeriesQueryIdUrl(seriesId);

        Log($"FetchSeriesById: {seriesId}: seriesUrl: {seriesUrl}");

        RequestResult requestResult = downloadClient.MakeRequest(seriesUrl, RequestType.MangaInfo);

        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            Log($"FetchSeriesById: {seriesId}: requestResult.statusCode: {requestResult.statusCode}");
            return null;
        }

        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);

        return result;
    }

    /**
     * @description: Get a list of Manga objects from a JsonArray. Used to convert the result of a search into Manga objects.
     * @param {JsonArray} series
     * @return Manga[]
     */
    private List<Manga> FetchMangasFromJsonArray(JsonArray series)
    {
        List<Manga> retManga = new List<Manga>();

        foreach (JsonObject? seriesData in series)
        {
            if (FetchMangaFromJsonObject(seriesData) is { } manga)
            {
                retManga.Add(manga);
            }
        }

        return retManga;
    }

    /**
     * @description: Get a Manga object from a JsonObject. Used to convert the result of a search into Manga objects.
     * @param {JsonObject} seriesData
     * @return Manga
     */
    private Manga? FetchMangaFromJsonObject(JsonObject? seriesData)
    {
        if (seriesData is null)
        {
            Log($"FetchMangaFromJsonObject: null seriesData");
            return null;
        }

        string? slug = seriesData["series_slug"]!.GetValue<string>();

        if (slug is null)
        {
            Log($"FetchMangaFromJsonObject: null slug");
            Log($"FetchMangaFromJsonObject: seriesData: {seriesData}");
            return null;
        }

        Manga? manga = FetchMangaBySeriesSlug(slug); // ! NB: FetchMangaBySeriesSlug fetches the manga from the series endpoint

        if (manga is null)
        {
            Log($"FetchMangaFromJsonObject: null manga");
            return null;
        }

        return manga;
    }

    private Manga ConvertSeriesToManga(JsonObject manga)
    {
        Log($"ConvertSeriesToManga");

        string title = manga["title"]!.GetValue<string>();

        string seriesSlug = manga["series_slug"]!.GetValue<string>();

        Log($"ConvertSeriesToManga: seriesSlug: {seriesSlug}");

        List<string> authors = manga["author"]!.GetValue<string>().Split(" & ").ToList();

        // description comes as html text, so we need to parse it
        string descriptionHtml = manga["description"]!.GetValue<string>();
        var htmlDoc = new HtmlDocument();
        htmlDoc.LoadHtml(descriptionHtml);
        string descriptionEscaped = htmlDoc.DocumentNode.InnerText;
        string description = WebUtility.HtmlDecode(descriptionEscaped);

        string altTitles = manga["alternative_names"]!.GetValue<string>();
        Dictionary<string, string> altTitlesDict = new Dictionary<string, string>{{"0", altTitles}};

        string[] tags = manga["tags"]!.AsArray().Where(tag => tag != null).Select(tag => tag!["name"]!.GetValue<string>()).ToArray();

        string? coverUrl = manga["thumbnail"]!.GetValue<string>();
        string? coverCacheName = null;
        if (coverUrl is not null)
            coverCacheName = SaveCoverImageToCache(coverUrl, seriesSlug, RequestType.MangaCover);

        Dictionary<string, string>? linksDict = null;

        int? year = null;
        string? yearStr = manga["release_year"]?.GetValue<string>();
        if (yearStr != null && int.TryParse(yearStr, out int parsedYear))
        {
            year = parsedYear;
        }

        string? originalLanguage = null;

        string status = manga["status"]!.GetValue<string>();
        Manga.ReleaseStatusByte releaseStatus = Manga.ReleaseStatusByte.Unreleased;
        switch (status.ToLower())
        {
            case "ongoing": releaseStatus = Manga.ReleaseStatusByte.Continuing; break;
            case "completed": releaseStatus = Manga.ReleaseStatusByte.Completed; break;
            case "hiatus": releaseStatus = Manga.ReleaseStatusByte.OnHiatus; break;
            case "cancelled": releaseStatus = Manga.ReleaseStatusByte.Cancelled; break;
        }

        string modifiedHostname = hostname.Replace("api.", "");
        string websiteUrl = $"{modifiedHostname}{mangaUrlPrefix}{seriesSlug}";

        Manga pub = new(
            title,
            authors,
            description,
            altTitlesDict,
            tags,
            coverUrl,
            coverCacheName,
            linksDict,
            year,
            originalLanguage,
            seriesSlug,
            releaseStatus,
            websiteUrl
        );
        string json = JsonSerializer.Serialize(pub, new System.Text.Json.JsonSerializerOptions{WriteIndented = true});
        Log($"ConvertSeriesToManga: {seriesSlug}: json: {json}");
        AddMangaToCache(pub);
        return pub;
    }

    private Chapter[] FetchChapters(Manga manga, string language="en")
    {
        Chapter[] chapterList = FetchChaptersV1(manga, language);

        if (chapterList.Length == 0) {
            chapterList = FetchChaptersV2(manga, language);
        }

        return chapterList;
    }

    private Chapter[] FetchChaptersV1(Manga manga, string language="en")
    {
        string seriesSlug = manga.publicationId;

        Log($"FetchChaptersV1: {seriesSlug}");

        JsonObject? result = FetchSeriesBySlug(seriesSlug);

        if (result is null)
        {
            Log($"FetchChaptersV1: {seriesSlug}: null result");
            return Array.Empty<Chapter>();
        }

        List<Chapter> retChapters = new();

        foreach (JsonObject? season in result["seasons"]!.AsArray())
        {
            if (season?["chapters"] is null)
            {
                continue;
            }

            foreach (JsonObject? chapter in season!["chapters"]!.AsArray())
            {
                // skip paid chapters
                if (chapter!["price"]!.GetValue<int>() > 0) {
                    continue;
                }

                Log($"FetchChaptersV1: {seriesSlug}: chapter: {chapter}");

                string chapterSlug = chapter!["chapter_slug"]!.GetValue<string>();
                string chapterTitle = chapter!["chapter_title"]?.GetValue<string>() ?? chapter!["chapter_name"]!.GetValue<string>();
                string chapterVolume = season!["index"]!.GetValue<int>().ToString();
                string chapterIndex = chapter!["index"]!.GetValue<string>();

                string chapterUrl = chapterSlug;
                string chapterId = chapterSlug;

                // NB: Hean doesn't have a chapter url (as far as i can tell), so we use the chapterSlug as the url
                // TODO: should chapterUrl / chapterId be different somehow?

                retChapters.Add(new Chapter(manga, chapterTitle, chapterVolume, chapterIndex, chapterUrl, chapterId));
            }
        }

        Log($"Got {retChapters.Count} chapters. {manga}");

        return retChapters.Order().ToArray();
    }

    private Chapter[] FetchChaptersV2(Manga manga, string language="en")
    {
        string seriesSlug = manga.publicationId;

        Log($"FetchChaptersV2: {seriesSlug}");

        JsonObject? resultBase = FetchSeriesBySlug(seriesSlug)!;

        string seriesId = resultBase["id"]!.GetValue<int>().ToString();

        Log($"FetchChaptersV2: {seriesSlug}: seriesId: {seriesId}");

        JsonObject? result = FetchSeriesById(seriesId);

        if (result is null)
        {
            Log($"FetchChaptersV2: {seriesSlug}: null result");
            return Array.Empty<Chapter>();
        }

        List<Chapter> retChapters = new();

        foreach (JsonObject? chapter in result["data"]!.AsArray())
        {
            string? chapterSlug = chapter!["chapter_slug"]!.GetValue<string>();

            if (chapterSlug is null)
            {
                Log($"FetchChaptersV2: {seriesSlug}: chapterSlug: {chapterSlug}: null chapterSlug");
                Log($"FetchChaptersV2: {seriesSlug}: chapter: {chapter}");
                continue;
            }

            // skip paid chapters
            if (chapter!["price"]!.GetValue<int>() > 0) {
                Log($"FetchChaptersV2: {seriesSlug}: chapterSlug: {chapterSlug}: skipping paid chapter");
                continue;
            }

            JsonObject? chapterData = FetchSeriesChapterBySlug(seriesSlug, chapterSlug);

            Log($"FetchChaptersV2: {seriesSlug}: chapterSlug: {chapterSlug}: got chapter data");

            string chapterTitle = GetChapterTitle(chapterData!["chapter"]!.AsObject());
            string? chapterVolume = GetChapterVolume(resultBase, chapterData!["chapter"]!.AsObject());
            string chapterIndex = chapterData!["chapter"]!["index"]!.GetValue<string>();

            string chapterUrl = chapterSlug;
            string chapterId = chapterSlug;

            // NB: Hean doesn't have a chapter url (as far as i can tell), so we use the chapterSlug as the url
            // TODO: should chapterUrl / chapterId be different somehow?

            retChapters.Add(new Chapter(manga, chapterTitle, chapterVolume, chapterIndex, chapterUrl, chapterId));
        }

        Log($"FetchChaptersV2: {seriesSlug}: retChapters.Count: {retChapters.Count}");

        return retChapters.Order().ToArray();
    }

    private string GetChapterTitle(JsonObject chapter)
    {
        string seriesSlug = chapter["series"]!["series_slug"]!.GetValue<string>();
        string chapterSlug = chapter["chapter_slug"]!.GetValue<string>();

        Log($"GetChapterTitle: {seriesSlug}: {chapterSlug}");

        string? title;

        title = chapter["chapter_title"]?.GetValue<string>();

        Log($"GetChapterTitle: {seriesSlug}: {chapterSlug}: (chapter_title): {title}");

        if (title == null) {
            title = chapter["chapter_name"]!.GetValue<string>();
            Log($"GetChapterTitle: {seriesSlug}: {chapterSlug}: (chapter_name): {title}");
        }

        Log($"GetChapterTitle: {seriesSlug}: {chapterSlug}: title: {title}");

        return title;
    }

    private string? GetChapterVolume(JsonObject seriesData, JsonObject chapterData)
    {
        string? volume;

        volume = GetChapterVolumeV1(chapterData);

        if (volume == null)
        {
            volume = GetChapterVolumeV2(seriesData, chapterData);
        }

        return volume;
    }

    private string? GetChapterVolumeV1(JsonObject chapterData)
    {
        string seriesSlug = chapterData["series"]!["series_slug"]!.GetValue<string>();
        string chapterSlug = chapterData["chapter_slug"]!.GetValue<string>();

        Log($"GetChapterVolumeV1: {seriesSlug}: {chapterSlug}");

        string? volume = chapterData["season"]?["index"]!.GetValue<int>().ToString();

        Log($"GetChapterVolumeV1: {seriesSlug}: {chapterSlug}: volume: {volume}");

        return volume;
    }

    private string? GetChapterVolumeV2(JsonObject seriesData, JsonObject chapterData)
    {
        string seriesSlug = chapterData["series"]!["series_slug"]!.GetValue<string>();
        string chapterSlug = chapterData["chapter_slug"]!.GetValue<string>();

        Log($"GetChapterVolumeV2: {seriesSlug}: {chapterSlug}");

        string? volume = null;

        Log($"GetChapterVolumeV2: {seriesSlug}: {chapterSlug}: seriesData: {seriesData}");
        Log($"GetChapterVolumeV2: {seriesSlug}: {chapterSlug}: chapterData: {chapterData}");

        int? seasonId = chapterData["season_id"]?.GetValue<int>();

        Log($"GetChapterVolumeV2: {seriesSlug}: {chapterSlug}: seasonId: {seasonId}");

        if (seasonId is null)
        {
            Log($"GetChapterVolumeV2: {seriesSlug}: {chapterSlug}: null seasonId");
            return null;
        }

        foreach (JsonObject? season in seriesData["seasons"]!.AsArray())
        {
            int thisSeasonId = season!["id"]!.GetValue<int>();

            Log($"GetChapterVolumeV2: {seriesSlug}: {chapterSlug}: thisSeasonId: {thisSeasonId}");

            if (thisSeasonId == seasonId)
            {
                int thisSeasonIndex = season!["index"]!.GetValue<int>();

                Log($"GetChapterVolumeV2: {seriesSlug}: {chapterSlug}: thisSeasonIndex: {thisSeasonIndex}");

                volume = thisSeasonIndex.ToString();

                break;
            }
        }

        Log($"GetChapterVolumeV2: {seriesSlug}: {chapterSlug}: volume: {volume}");

        return volume;
    }

    private JsonObject? FetchSeriesChapterBySlug(string seriesSlug, string chapterSlug)
    {
        Log($"FetchSeriesChapterBySlug: {seriesSlug}: {chapterSlug}");

        string chapterUrl = GetPublicationChapterSlugUrl(seriesSlug, chapterSlug);

        Log($"FetchSeriesChapterBySlug: {seriesSlug}: {chapterSlug}: chapterUrl: {chapterUrl}");

        RequestResult requestResult = downloadClient.MakeRequest(chapterUrl, RequestType.MangaDexImage);

        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            Log($"FetchSeriesChapterBySlug: {seriesSlug}: {chapterSlug}: requestResult.statusCode: {requestResult.statusCode}");
            return new JsonObject();
        }

        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);

        return result;
    }

    private HttpStatusCode FetchChapterImages(Chapter chapter, ProgressToken? progressToken = null)
    {
        string seriesSlug = chapter.parentManga.publicationId;
        string? chapterSlug = chapter.id;

        Log($"FetchChapterImages: {seriesSlug}: {chapterSlug}");

        if (progressToken?.cancellationRequested == true)
        {
            Log($"FetchChapterImages: {seriesSlug}: {chapterSlug}: cancelled");
            progressToken.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        if (chapterSlug is null)
        {
            Log($"FetchChapterImages: {seriesSlug}: {chapterSlug}: null chapterSlug");
            progressToken?.Cancel();
            return HttpStatusCode.BadRequest;
        }

        JsonObject? seriesChapterData = FetchSeriesChapterBySlug(seriesSlug, chapterSlug);

        if (seriesChapterData is null)
        {
            Log($"FetchChapterImages: {seriesSlug}: {chapterSlug}: null result");
            progressToken?.Cancel();
            return HttpStatusCode.NoContent;    // TODO: we probably want to return a 4xx status code here
        }

        if (seriesChapterData["paywall"]?.GetValue<bool>() == true)
        {
            Log($"FetchChapterImages: {seriesSlug}: {chapterSlug}: skipping paywalled chapter");
            progressToken?.Cancel();
            return HttpStatusCode.PaymentRequired;
        }

        JsonArray? chapterImageList = (JsonArray?)seriesChapterData["data"];

        if (chapterImageList is null)
        {
            Log($"FetchChapterImages: {seriesSlug}: {chapterSlug}: response.data is null, trying response.chapter.chapter_data.images");
            chapterImageList = (JsonArray?)seriesChapterData["chapter"]?["chapter_data"]?["images"];
        }

        if (chapterImageList is null)
        {
            Log($"FetchChapterImages: {seriesSlug}: {chapterSlug}: response.chapter.chapter_data.images is null, unable to find images");
            Log($"FetchChapterImages: {seriesSlug}: {chapterSlug}: result: {seriesChapterData}");
            progressToken?.Cancel();
            return HttpStatusCode.NoContent;    // ? TODO: we probably want to return a 4xx status code here
        }

        Chapter.ChapterImages chapterImages = new();

        if (seriesChapterData["chapter"]!["series"]?["thumbnail"]?.GetValue<string>() is string seriesThumbnail)
        {
            chapterImages.Cover = ConvertRelativeUrlToAbsolute(seriesThumbnail);
        }

        if (seriesChapterData["chapter"]!["chapter_thumbnail"]?.GetValue<string>() is string chapterThumbnail)
        {
            chapterImages.Thumbnail = ConvertRelativeUrlToAbsolute(chapterThumbnail);
        }

        chapterImages.StoryPages = chapterImageList.Select(node => ConvertRelativeUrlToAbsolute(node!.GetValue<string>())).ToArray();

        return DownloadChapterImages(chapterImages, chapter, RequestType.MangaImage, progressToken:progressToken);
    }

    private List<string> ConvertRelativeUrlToAbsolute(List<string> imageUrls)
    {
        List<string> ret = new();

        for (int i = 0; i < imageUrls.Count; i++)
        {
            ret.Add(ConvertRelativeUrlToAbsolute(imageUrls[i]));
        }

        return ret;
    }

    private string ConvertRelativeUrlToAbsolute(string imageUrl)
    {
        Uri baseUri = new(hostname);
        Uri uri = new(baseUri, imageUrl);   // baseUri is ignored if imageUrl is absolute
        return uri.ToString();
    }
}
