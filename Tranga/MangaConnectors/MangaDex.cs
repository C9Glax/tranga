using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Tranga.Jobs;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Tranga.MangaConnectors;
public class MangaDex : MangaConnector
{
    public MangaDex(GlobalBase clone) : base(clone, "MangaDex")
    {
        this.downloadClient = new HttpDownloadClient(clone);
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        const int limit = 100; //How many values we want returned at once
        int offset = 0; //"Page"
        int total = int.MaxValue; //How many total results are there, is updated on first request
        HashSet<Manga> retManga = new();
        int loadedPublicationData = 0;
        List<JsonNode> results = new();
        
        //Request all search-results
        while (offset < total) //As long as we haven't requested all "Pages"
        {
            //Request next Page
            RequestResult requestResult = downloadClient.MakeRequest(
                    $"https://api.mangadex.org/manga?limit={limit}&title={publicationTitle}&offset={offset}" +
                    $"&contentRating%5B%5D=safe&contentRating%5B%5D=suggestive&contentRating%5B%5D=erotica" +
                    $"&contentRating%5B%5D=pornographic" +
                    $"&includes%5B%5D=manga&includes%5B%5D=cover_art&includes%5B%5D=author" +
                    $"&includes%5B%5D=artist&includes%5B%5D=tag", RequestType.MangaInfo);
            if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
                break;
            JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
            
            offset += limit;
            if (result is null)
                break;
            
            if(result.ContainsKey("total"))
                total = result["total"]!.GetValue<int>(); //Update the total number of Publications
            else continue;

            if (result.ContainsKey("data"))
                results.AddRange(result["data"]!.AsArray()!);//Manga-data-Array
        }
        
        foreach (JsonNode mangaNode in results)
        {
            Log($"Getting publication data. {++loadedPublicationData}/{total}");
            if(MangaFromJsonObject(mangaNode.AsObject()) is { } manga)
                retManga.Add(manga); //Add Publication (Manga) to result
        }
        Log($"Retrieved {retManga.Count} publications. Term=\"{publicationTitle}\"");
        return retManga.ToArray();
    }

    public override Manga? GetMangaFromId(string publicationId)
    {
        RequestResult requestResult =
            downloadClient.MakeRequest($"https://api.mangadex.org/manga/{publicationId}", RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return null;
        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
        if(result is not null)
            return MangaFromJsonObject(result["data"]!.AsObject());
        return null;
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        Regex idRex = new (@"https:\/\/mangadex.org\/title\/([A-z0-9-]*)\/.*");
        string id = idRex.Match(url).Groups[1].Value;
        Log($"Got id {id} from {url}");
        return GetMangaFromId(id);
    }

    private Manga? MangaFromJsonObject(JsonObject manga)
    {
        if (!manga.TryGetPropertyValue("id", out JsonNode? idNode))
            return null;
        string publicationId = idNode!.GetValue<string>();
            
        if (!manga.TryGetPropertyValue("attributes", out JsonNode? attributesNode))
            return null;
        JsonObject attributes = attributesNode!.AsObject();

        if (!attributes.TryGetPropertyValue("title", out JsonNode? titleNode))
            return null;
        string title = titleNode!.AsObject().ContainsKey("en") switch
        {
            true => titleNode.AsObject()["en"]!.GetValue<string>(),
            false => titleNode.AsObject().First().Value!.GetValue<string>()
        };
        
        Dictionary<string, string> altTitlesDict = new();
        if (attributes.TryGetPropertyValue("altTitles", out JsonNode? altTitlesNode))
        {
            foreach (JsonNode? altTitleNode in altTitlesNode!.AsArray())
            {
                JsonObject altTitleNodeObject = altTitleNode!.AsObject();
                altTitlesDict.TryAdd(altTitleNodeObject.First().Key, altTitleNodeObject.First().Value!.GetValue<string>());
            }
        }

        if (!attributes.TryGetPropertyValue("description", out JsonNode? descriptionNode))
            return null;
        string description = descriptionNode!.AsObject().ContainsKey("en") switch
        {
            true => descriptionNode.AsObject()["en"]!.GetValue<string>(),
            false => descriptionNode.AsObject().FirstOrDefault().Value?.GetValue<string>() ?? ""
        };

        Dictionary<string, string> linksDict = new();
        if (attributes.TryGetPropertyValue("links", out JsonNode? linksNode))
            foreach (KeyValuePair<string, JsonNode> linkKv in linksNode!.AsObject())
                linksDict.TryAdd(linkKv.Key, linkKv.Value.GetValue<string>());

        string? originalLanguage =
            attributes.TryGetPropertyValue("originalLanguage", out JsonNode? originalLanguageNode) switch
            {
                true => originalLanguageNode?.GetValue<string>(),
                false => null
            };
        
        Manga.ReleaseStatusByte status = Manga.ReleaseStatusByte.Unreleased;
        if (attributes.TryGetPropertyValue("status", out JsonNode? statusNode))
        {
            status = statusNode?.GetValue<string>().ToLower() switch
            {
                "ongoing" => Manga.ReleaseStatusByte.Continuing,
                "completed" => Manga.ReleaseStatusByte.Completed,
                "hiatus" => Manga.ReleaseStatusByte.OnHiatus,
                "cancelled" => Manga.ReleaseStatusByte.Cancelled,
                _ => Manga.ReleaseStatusByte.Unreleased
            };
        }

        int? year = attributes.TryGetPropertyValue("year", out JsonNode? yearNode) switch
        {
            true => yearNode?.GetValue<int>(),
            false => null
        };
        
        HashSet<string> tags = new(128);
        if (attributes.TryGetPropertyValue("tags", out JsonNode? tagsNode))
            foreach (JsonNode? tagNode in tagsNode!.AsArray())
                tags.Add(tagNode!["attributes"]!["name"]!["en"]!.GetValue<string>());

        
        if (!manga.TryGetPropertyValue("relationships", out JsonNode? relationshipsNode))
            return null;
        
        JsonNode? coverNode = relationshipsNode!.AsArray()
            .FirstOrDefault(rel => rel!["type"]!.GetValue<string>().Equals("cover_art"));
        if (coverNode is null)
            return null;
        string fileName = coverNode["attributes"]!["fileName"]!.GetValue<string>();
        string coverUrl = $"https://uploads.mangadex.org/covers/{publicationId}/{fileName}";
        string coverCacheName = SaveCoverImageToCache(coverUrl, RequestType.MangaCover);
        
        List<string> authors = new();
        JsonNode?[] authorNodes = relationshipsNode.AsArray()
            .Where(rel => rel!["type"]!.GetValue<string>().Equals("author") || rel!["type"]!.GetValue<string>().Equals("artist")).ToArray();
        foreach (JsonNode? authorNode in authorNodes)
        {
            string authorName = authorNode!["attributes"]!["name"]!.GetValue<string>();
            if(!authors.Contains(authorName))
               authors.Add(authorName);
        }

        Manga pub = new(
            title,
            authors,
            description,
            altTitlesDict,
            tags.ToArray(),
            coverUrl,
            coverCacheName,
            linksDict,
            year,
            originalLanguage,
            Enum.GetName(status) ?? "",
            publicationId,
            status
        );
        cachedPublications.Add(pub);
        return pub;
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        Log($"Getting chapters {manga}");
        const int limit = 100; //How many values we want returned at once
        int offset = 0; //"Page"
        int total = int.MaxValue; //How many total results are there, is updated on first request
        List<Chapter> chapters = new();
        //As long as we haven't requested all "Pages"
        while (offset < total)
        {
            //Request next "Page"
            RequestResult requestResult =
                downloadClient.MakeRequest(
                    $"https://api.mangadex.org/manga/{manga.publicationId}/feed?limit={limit}&offset={offset}&translatedLanguage%5B%5D={language}&contentRating%5B%5D=safe&contentRating%5B%5D=suggestive&contentRating%5B%5D=erotica&contentRating%5B%5D=pornographic", RequestType.MangaDexFeed);
            if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
                break;
            JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
            
            offset += limit;
            if (result is null)
                break;
            
            total = result["total"]!.GetValue<int>();
            JsonArray chaptersInResult = result["data"]!.AsArray();
            //Loop through all Chapters in result and extract information from JSON
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
                
                string chapterNum = attributes.ContainsKey("chapter") && attributes["chapter"] is not null
                    ? attributes["chapter"]!.GetValue<string>()
                    : "null";
                
                
                if (attributes.ContainsKey("pages") && attributes["pages"] is not null &&
                    attributes["pages"]!.GetValue<int>() < 1)
                {
                    Log($"Skipping {chapterId} Vol.{volume} Ch.{chapterNum} {title} because it has no pages or is externally linked.");
                    continue;
                }
                
                if(chapterNum is not "null")
                    chapters.Add(new Chapter(manga, title, volume, chapterNum, chapterId));
            }
        }

        //Return Chapters ordered by Chapter-Number
        Log($"Got {chapters.Count} chapters. {manga}");
        return chapters.Order().ToArray();
    }

    public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
    {
        if (progressToken?.cancellationRequested ?? false)
        {
            progressToken.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Manga chapterParentManga = chapter.parentManga;
        Log($"Retrieving chapter-info {chapter} {chapterParentManga}");
        //Request URLs for Chapter-Images
        RequestResult requestResult =
            downloadClient.MakeRequest($"https://api.mangadex.org/at-home/server/{chapter.url}?forcePort443=false", RequestType.MangaDexImage);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            progressToken?.Cancel();
            return requestResult.statusCode;
        }
        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
        if (result is null)
        {
            progressToken?.Cancel();
            return HttpStatusCode.NoContent;
        }

        string baseUrl = result["baseUrl"]!.GetValue<string>();
        string hash = result["chapter"]!["hash"]!.GetValue<string>();
        JsonArray imageFileNames = result["chapter"]!["data"]!.AsArray();
        //Loop through all imageNames and construct urls (imageUrl)
        HashSet<string> imageUrls = new();
        foreach (JsonNode? image in imageFileNames)
            imageUrls.Add($"{baseUrl}/data/{hash}/{image!.GetValue<string>()}");

        string comicInfoPath = Path.GetTempFileName();
        File.WriteAllText(comicInfoPath, chapter.GetComicInfoXmlString());
        
        //Download Chapter-Images
        return DownloadChapterImages(imageUrls.ToArray(), chapter.GetArchiveFilePath(settings.downloadLocation), RequestType.MangaImage, comicInfoPath, progressToken:progressToken);
    }
}