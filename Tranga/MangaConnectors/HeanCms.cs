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

    public HeanCms(GlobalBase clone, string label) : base(clone, label)
    {
        this.downloadClient = new HttpDownloadClient(clone);
    }

    public override Manga[] GetManga(String publicationTitle = "")
    {
        Log($"GetManga: {publicationTitle}");
        try {
            return FetchMangasByTitle(publicationTitle);
        } catch (Exception e) {
            Log($"Failed to get manga. {publicationTitle} {e}");
            return Array.Empty<Manga>();
        }
    }

    public override Manga? GetMangaFromId(string publicationId)
    {
        Log($"GetMangaFromId: {publicationId}");
        try {
            return FetchMangaById(publicationId);
        } catch (Exception e) {
            Log($"Failed to get manga from id. {publicationId} {e}");
            return null;
        }
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        Log($"GetMangaFromUrl: {url}");

        string prefix = "/series/";
        int startIndex = url.IndexOf(prefix) + prefix.Length;
        string id = url.Substring(startIndex);

        Log($"Got id {id} from {url}");
        return GetMangaFromId(id);
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        Log($"GetChapters: {manga.publicationId}");
        try {
            return FetchChapters(manga);
        } catch (Exception e) {
            Log($"Failed to get chapters. {manga.publicationId} {e}");
            return Array.Empty<Chapter>();
        }
    }

    public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
    {
        Log($"DownloadChapter: {chapter.parentManga.publicationId}: {chapter.url}");
        try {
            return FetchChapterImages(chapter, progressToken);
        } catch (Exception e) {
            Log($"Failed to download chapter. {chapter.parentManga.publicationId}: {chapter.url} {e}");
            return HttpStatusCode.InternalServerError;
        }
    }

    private readonly string queryPath = $"/query?query_string=";
    private readonly string seriesPath = $"/series";
    private readonly string chapterPath = $"/chapter";

    private string GetQueryUrl(string publicationTitle) => $"{hostname}{queryPath}?query_string={publicationTitle}";
    private string GetSeriesUrl(string publicationSlug) => $"{hostname}{seriesPath}/{publicationSlug}";
    private string GetSeriesQueryUrl(string publicationId) => $"{hostname}{chapterPath}/query?series_id={publicationId}&perPage=9999&page=1";
    private string GetChapterSlugUrl(string publicationSlug, string chapterSlug) => $"{hostname}{chapterPath}/{publicationSlug}/{chapterSlug}";
    private string GetChapterIdUrl(string chapterId) => $"{hostname}{chapterPath}/{chapterId}";

    private Manga[] FetchMangasByTitle(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");

        Manga[]? searchResults = SearchMangasByTitle(publicationTitle);

        if (searchResults is null)
        {
            Log($"searchResults is null Term=\"{publicationTitle}\"");
            return Array.Empty<Manga>();
        }

        return searchResults;
    }

    private Manga? FetchMangaById(string publicationId)
    {
        Log($"Getting manga from id. {publicationId}");

        JsonObject? result = FetchSeriesBySlug(publicationId);

        if (result is null)
        {
            Log($"Failed to get manga from id. {publicationId}");
            return null;
        }

        Manga retManga = ConvertSeriesToManga(result);

        return retManga;
    }

    private Manga[]? SearchMangasByTitle(string searchQuery)
    {
        string queryUrl = GetQueryUrl(searchQuery);

        RequestResult requestResult = downloadClient.MakeRequest(queryUrl, RequestType.MangaInfo);

        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            Log($"Failed to get query. Term=\"{searchQuery}\" Status: {requestResult.statusCode}");
            return null;
        }

        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);

        if (result is null)
        {
            Log($"Got a null response. Term=\"{searchQuery}\"");
            return null;
        }

        JsonArray series = result["data"]!.AsArray();

        if (series is null)
        {
            Log($"Got a empty data. Term=\"{searchQuery}\"");
            return null;
        }

        List<Manga> retManga = FetchMangasFromJsonArray(series);

        return retManga.ToArray();
    }

    private JsonObject? FetchSeriesBySlug(string publicationSlug)
    {
        Log($"Getting series {publicationSlug}");

        string seriesUrl = GetSeriesUrl(publicationSlug);

        RequestResult requestResult = downloadClient.MakeRequest(seriesUrl, RequestType.MangaInfo);

        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            Log($"Failed to get series. Term=\"{publicationSlug}\" Status: {requestResult.statusCode}");
            return null;
        }

        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);

        return result;
    }

    private JsonObject? FetchSeriesById(string id)
    {
        Log($"Getting series by query {id}");

        string seriesUrl = GetSeriesQueryUrl(id);

        RequestResult requestResult = downloadClient.MakeRequest(seriesUrl, RequestType.MangaInfo);

        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            Log($"Failed to get series. Term=\"{id}\" Status: {requestResult.statusCode}");
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
            Log($"Got a null seriesData.");
            return null;
        }

        string? slug = seriesData["series_slug"]!.GetValue<string>();

        if (slug is null)
        {
            Log($"Got a null slug. {seriesData}");
            return null;
        }

        Manga? manga = FetchMangaById(slug); // ! NB: FetchMangaById fetches the manga from the series endpoint

        if (manga is null)
        {
            Log($"Failed to get manga from id. {slug}");
            return null;
        }

        return manga;
    }

    private Manga ConvertSeriesToManga(JsonObject manga)
    {
        Log($"Converting series to manga");

        string title = manga["title"]!.GetValue<string>();

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
            coverCacheName = SaveCoverImageToCache(coverUrl, RequestType.MangaCover);

        Dictionary<string, string>? linksDict = null;

        int? year = null;
        string? yearStr = manga["release_year"]?.GetValue<string>();
        if (yearStr != null && int.TryParse(yearStr, out int parsedYear))
        {
            year = parsedYear;
        }

        string? originalLanguage = null;

        string publicationId = manga["series_slug"]!.GetValue<string>();

        Log($"Got publicationId {publicationId}");

        string status = manga["status"]!.GetValue<string>();
        Manga.ReleaseStatusByte releaseStatus = Manga.ReleaseStatusByte.Unreleased;
        switch (status.ToLower())
        {
            case "ongoing": releaseStatus = Manga.ReleaseStatusByte.Continuing; break;
            case "completed": releaseStatus = Manga.ReleaseStatusByte.Completed; break;
            case "hiatus": releaseStatus = Manga.ReleaseStatusByte.OnHiatus; break;
            case "cancelled": releaseStatus = Manga.ReleaseStatusByte.Cancelled; break;
        }

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
            publicationId,
            releaseStatus
            // TODO: websiteUrl
            // string? websiteUrl = null
        );
        string json = JsonSerializer.Serialize(pub, new System.Text.Json.JsonSerializerOptions{WriteIndented = true});
        Log($"Converted series to manga. {json}");
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
        Log($"Fetching chapters v1 {manga.publicationId}");

        JsonObject? result = FetchSeriesBySlug(manga.publicationId);

        Log($"Got series {manga.publicationId}");

        if (result is null)
        {
            Log($"Failed to get chapters - null result. {manga}");
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
                if (chapter!["price"]!.GetValue<int>() > 0)
                    continue;

                Log($"Got chapter {chapter}");

                string chapterId = chapter!["chapter_slug"]!.GetValue<string>();
                string title = chapter!["chapter_title"]?.GetValue<string>() ?? chapter!["chapter_name"]!.GetValue<string>();
                string volume = season!["index"]!.GetValue<int>().ToString();
                string chapterNum = chapter!["index"]!.GetValue<string>();

                // NB: Hean doesn't have a chapter url (as far as i can tell), so we use the chapterId as the url

                retChapters.Add(new Chapter(manga, title, volume, chapterNum, chapterId));
            }
        }

        Log($"Got {retChapters.Count} chapters. {manga}");

        return retChapters.Order().ToArray();
    }

    private Chapter[] FetchChaptersV2(Manga manga, string language="en")
    {
        Log($"Fetching chapters v2 {manga.publicationId}");

        JsonObject? resultBase = FetchSeriesBySlug(manga.publicationId)!;

        string publicationId = resultBase["id"]!.GetValue<int>().ToString();

        JsonObject? result = FetchSeriesById(publicationId);

        Log($"Got series {manga.publicationId}");

        if (result is null)
        {
            Log($"Failed to get chapters - null result. {manga}");
            return Array.Empty<Chapter>();
        }

        List<Chapter> retChapters = new();

        foreach (JsonObject? chapter in result["data"]!.AsArray())
        {
            // skip paid chapters
            if (chapter!["price"]!.GetValue<int>() > 0)
                continue;

            string chapterId = chapter["id"]!.GetValue<int>().ToString();

            JsonObject? chapterData = FetchChapterById(chapterId);

            Log($"Got chapterData {chapterData}");

            string chapterSlug = chapterData!["chapter_slug"]!.GetValue<string>();
            string title = chapterData!["chapter_title"]?.GetValue<string>() ?? chapter!["chapter_name"]!.GetValue<string>();
            string volume = chapterData!["season"]!["index"]!.GetValue<int>().ToString();
            string chapterNum = chapterData!["index"]!.GetValue<string>();

            retChapters.Add(new Chapter(manga, title, volume, chapterNum, chapterSlug));
        }

        Log($"Got {retChapters.Count} chapters. {manga}");

        return retChapters.Order().ToArray();

    }

    private JsonObject? FetchChapterBySlug(string publicationId, string chapterId)
    {
        Log($"Fetching chapter {publicationId} {chapterId}");

        string chapterUrl = GetChapterSlugUrl(publicationId, chapterId);

        Log($"Fetching chapter {chapterUrl}");

        RequestResult requestResult = downloadClient.MakeRequest(chapterUrl, RequestType.MangaDexImage);

        Log($"Got chapter {publicationId} {chapterId} {requestResult.statusCode}");

        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            Log($"Failed to get chapter. {publicationId} {chapterId} Status: {requestResult.statusCode}");
            return new JsonObject();
        }

        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);

        Log($"Got chapter {publicationId} {chapterId} {result}");

        return result;
    }

    private JsonObject? FetchChapterById(string chapterId)
    {
        Log($"Fetching chapter by id {chapterId}");

        string chapterUrl = GetChapterIdUrl(chapterId);

        Log($"Fetching chapter {chapterUrl}");

        RequestResult requestResult = downloadClient.MakeRequest(chapterUrl, RequestType.MangaInfo);

        Log($"Got chapter {chapterId} {requestResult.statusCode}");

        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            Log($"Failed to get chapter. {chapterId} Status: {requestResult.statusCode}");
            return new JsonObject();
        }

        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);

        Log($"Got chapter {chapterId} {result}");

        return result;
    }

    private HttpStatusCode FetchChapterImages(Chapter chapter, ProgressToken? progressToken = null)
    {
        if (progressToken?.cancellationRequested ?? false)
        {
            Log($"DownloadChapter cancelled. {chapter}");
            progressToken.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Manga chapterParentManga = chapter.parentManga;

        Log($"Retrieving chapter-info {chapter} {chapterParentManga}");

        JsonObject? result = FetchChapterBySlug(chapterParentManga.publicationId, chapter.url);

        if (result is null)
        {
            Log($"Got a null response. {chapter} {chapterParentManga}");
            progressToken?.Cancel();
            return HttpStatusCode.NoContent;
        }

        if (result["paywall"]?.GetValue<bool>() ?? false)
        {
            Log($"Chapter is behind a paywall. {chapter} {chapterParentManga}");
            progressToken?.Cancel();
            return HttpStatusCode.PaymentRequired;
        }

        JsonArray? data = (JsonArray?)result["data"];

        if (data is null)
        {
            data = (JsonArray?)result["chapter"]?["chapter_data"]?["images"];
        }

        if (data is null)
        {
            Log($"Got a null data. {chapter} {chapterParentManga}");
            progressToken?.Cancel();
            return HttpStatusCode.NoContent;
        }

        List<string> imageUrls = data
            .Select(node => node!.GetValue<string>())
            .ToList();

        if (result["chapter_thumbnail"]?.GetValue<string>() is string chapterThumbnail)
        {
            imageUrls.Insert(0, chapterThumbnail);
        }

        imageUrls = ConvertRelativeUrlsToStatic(imageUrls);

        string comicInfoPath = Path.GetTempFileName();
        File.WriteAllText(comicInfoPath, chapter.GetComicInfoXmlString());

        return DownloadChapterImages(imageUrls.ToArray(), chapter.GetArchiveFilePath(settings.downloadLocation), RequestType.MangaImage, comicInfoPath, progressToken:progressToken);
    }

    private List<string> ConvertRelativeUrlsToStatic(List<string> imageUrls)
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