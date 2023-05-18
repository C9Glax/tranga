using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Tranga.Connectors;
//TODO Download covers: https://api.mangadex.org/docs/retrieving-covers/ https://api.mangadex.org/docs/swagger.html#/

public class MangaDex : Connector
{
    public override string name { get; }
    private readonly DownloadClient _downloadClient = new (750);

    public MangaDex(string downloadLocation) : base(downloadLocation)
    {
        name = "MangaDex.org";
    }

    public override Publication[] GetPublications(string publicationTitle = "")
    {
        const int limit = 100;
        int offset = 0;
        int total = int.MaxValue;
        HashSet<Publication> publications = new();
        while (offset < total)
        {
            DownloadClient.RequestResult requestResult = _downloadClient.MakeRequest($"https://api.mangadex.org/manga?limit={limit}&title={publicationTitle}&offset={offset}");
            JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
            offset += limit;
            if (result is null)
                break;
            
            total = result["total"]!.GetValue<int>();
            JsonArray mangaInResult = result["data"]!.AsArray();
            foreach (JsonNode? mangeNode in mangaInResult)
            {
                JsonObject manga = (JsonObject)mangeNode!;
                JsonObject attributes = manga["attributes"]!.AsObject();
                
                string title = attributes["title"]!.AsObject().ContainsKey("en") && attributes["title"]!["en"] is not null
                    ? attributes["title"]!["en"]!.GetValue<string>()
                    : "";
                
                string? description = attributes["description"]!.AsObject().ContainsKey("en") && attributes["description"]!["en"] is not null
                    ? attributes["description"]!["en"]!.GetValue<string?>()
                    : null;

                JsonArray altTitlesObject = attributes["altTitles"]!.AsArray();
                string[,] altTitles = new string[altTitlesObject.Count, 2];
                int titleIndex = 0;
                foreach (JsonNode? altTitleNode in altTitlesObject)
                {
                    JsonObject altTitleObject = (JsonObject)altTitleNode!;
                    string key = ((IDictionary<string, JsonNode?>)altTitleObject).Keys.ToArray()[0];
                    altTitles[titleIndex, 0] = key;
                    altTitles[titleIndex++, 1] = altTitleObject[key]!.GetValue<string>();
                }

                JsonArray tagsObject = attributes["tags"]!.AsArray();
                HashSet<string> tags = new();
                foreach (JsonNode? tagNode in tagsObject)
                {
                    JsonObject tagObject = (JsonObject)tagNode!;
                    if(tagObject["attributes"]!["name"]!.AsObject().ContainsKey("en"))
                        tags.Add(tagObject["attributes"]!["name"]!["en"]!.GetValue<string>());
                }

                string? poster = null;
                if (manga.ContainsKey("relationships") && manga["relationships"] is not null)
                {
                    JsonArray relationships = manga["relationships"]!.AsArray();
                    poster = relationships.FirstOrDefault(relationship => relationship!["type"]!.GetValue<string>() == "cover_art")!["id"]!.GetValue<string>();
                }

                string[,]? links = null;
                if (attributes.ContainsKey("links") && attributes["links"] is not null)
                {
                    JsonObject linksObject = attributes["links"]!.AsObject();
                    links = new string[linksObject.Count, 2];
                    int linkIndex = 0;
                    foreach (string key in ((IDictionary<string, JsonNode?>)linksObject).Keys)
                    {
                        links[linkIndex, 0] = key;
                        links[linkIndex++, 1] = linksObject[key]!.GetValue<string>();
                    }
                }
                
                int? year = attributes.ContainsKey("year") && attributes["year"] is not null
                    ? attributes["year"]!.GetValue<int?>()
                    : null;

                string? originalLanguage = attributes.ContainsKey("originalLanguage") && attributes["originalLanguage"] is not null
                    ? attributes["originalLanguage"]!.GetValue<string?>()
                    : null;
                
                string status = attributes["status"]!.GetValue<string>();

                Publication pub = new Publication(
                    title,
                    description,
                    altTitles,
                    tags.ToArray(),
                    poster,
                    links,
                    year,
                    originalLanguage,
                    status,
                    this,
                    manga["id"]!.GetValue<string>()
                );
                publications.Add(pub);
            }
        }

        return publications.ToArray();
    }

    public override Chapter[] GetChapters(Publication publication, string language = "")
    {
        const int limit = 100;
        int offset = 0;
        string id = publication.downloadUrl;
        int total = int.MaxValue;
        List<Chapter> chapters = new();
        while (offset < total)
        {
            DownloadClient.RequestResult requestResult =
                _downloadClient.MakeRequest($"https://api.mangadex.org/manga/{id}/feed?limit={limit}&offset={offset}&translatedLanguage%5B%5D={language}");
            JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
            
            offset += limit;
            if (result is null)
                break;
            
            total = result["total"]!.GetValue<int>();
            JsonArray chaptersInResult = result["data"]!.AsArray();
            foreach (JsonNode? jsonNode in chaptersInResult)
            {
                JsonObject chapter = (JsonObject)jsonNode!;
                JsonObject attributes = chapter["attributes"]!.AsObject();
                string chapterId = chapter["id"]!.GetValue<string>();
                
                string? title = attributes.ContainsKey("title") && attributes["title"] is not null
                    ? attributes["title"]!.GetValue<string>()
                    : null;
                
                string? volume = attributes.ContainsKey("volume") && attributes["volume"] is not null
                    ? attributes["volume"]!.GetValue<string>()
                    : null;
                
                string? chapterNum = attributes.ContainsKey("chapter") && attributes["chapter"] is not null
                    ? attributes["chapter"]!.GetValue<string>()
                    : null;
                
                string chapterName = string.Concat((title ?? "").Split(Path.GetInvalidFileNameChars()));
                string chapterFileName = $"{chapterName} - V{volume}C{chapterNum}";
                
                chapters.Add(new Chapter(publication, title, volume, chapterNum, chapterId, chapterFileName));
            }
        }

        NumberFormatInfo chapterNumberFormatInfo = new()
        {
            NumberDecimalSeparator = "."
        };
        return chapters.OrderBy(chapter => Convert.ToSingle(chapter.chapterNumber, chapterNumberFormatInfo)).ToArray();
    }

    public override void DownloadChapter(Publication publication, Chapter chapter)
    {
        DownloadClient.RequestResult requestResult =
            _downloadClient.MakeRequest($"https://api.mangadex.org/at-home/server/{chapter.url}?forcePort443=false'");
        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
        if (result is null)
            return;

        string baseUrl = result["baseUrl"]!.GetValue<string>();
        string hash = result["chapter"]!["hash"]!.GetValue<string>();
        JsonArray imageFileNames = result["chapter"]!["data"]!.AsArray();
        HashSet<string> imageUrls = new();
        foreach (JsonNode? image in imageFileNames)
            imageUrls.Add($"{baseUrl}/data/{hash}/{image!.GetValue<string>()}");

        DownloadChapter(imageUrls.ToArray(), Path.Join(downloadLocation, publication.folderName, chapter.fileName));
    }

    protected override void DownloadImage(string url, string savePath)
    {
        DownloadClient.RequestResult requestResult = _downloadClient.MakeRequest(url);
        byte[] buffer = new byte[requestResult.result.Length];
        requestResult.result.ReadExactly(buffer, 0, buffer.Length);
        File.WriteAllBytes(savePath, buffer);
    }
}