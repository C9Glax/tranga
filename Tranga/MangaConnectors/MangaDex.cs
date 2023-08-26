﻿using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;
public class MangaDex : MangaConnector
{
    public override string name { get; }

    private enum RequestType : byte
    {
        Manga,
        Feed,
        AtHomeServer,
        CoverUrl,
        Author,
    }

    public MangaDex(GlobalBase clone) : base(clone)
    {
        name = "MangaDex";
        this.downloadClient = new DownloadClient(clone, new Dictionary<byte, int>()
        {
            {(byte)RequestType.Manga, 250},
            {(byte)RequestType.Feed, 250},
            {(byte)RequestType.AtHomeServer, 40},
            {(byte)RequestType.CoverUrl, 250},
            {(byte)RequestType.Author, 250}
        });
    }

    public override Publication[] GetPublications(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        const int limit = 100; //How many values we want returned at once
        int offset = 0; //"Page"
        int total = int.MaxValue; //How many total results are there, is updated on first request
        HashSet<Publication> publications = new();
        int loadedPublicationData = 0;
        while (offset < total) //As long as we haven't requested all "Pages"
        {
            //Request next Page
            DownloadClient.RequestResult requestResult =
                downloadClient.MakeRequest(
                    $"https://api.mangadex.org/manga?limit={limit}&title={publicationTitle}&offset={offset}", (byte)RequestType.Manga);
            if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
                break;
            JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
            
            offset += limit;
            if (result is null)
                break;
            
            total = result["total"]!.GetValue<int>(); //Update the total number of Publications
            
            JsonArray mangaInResult = result["data"]!.AsArray(); //Manga-data-Array
            //Loop each Manga and extract information from JSON
            foreach (JsonNode? mangeNode in mangaInResult)
            {
                Log($"Getting publication data. {++loadedPublicationData}/{total}");
                JsonObject manga = (JsonObject)mangeNode!;
                JsonObject attributes = manga["attributes"]!.AsObject();
                
                string publicationId = manga["id"]!.GetValue<string>();
                
                string title = attributes["title"]!.AsObject().ContainsKey("en") && attributes["title"]!["en"] is not null
                    ? attributes["title"]!["en"]!.GetValue<string>()
                    : attributes["title"]![((IDictionary<string, JsonNode?>)attributes["title"]!.AsObject()).Keys.First()]!.GetValue<string>();

                string? description = attributes["description"]!.AsObject().ContainsKey("en") && attributes["description"]!["en"] is not null
                    ? attributes["description"]!["en"]!.GetValue<string?>()
                    : null;

                JsonArray altTitlesObject = attributes["altTitles"]!.AsArray();
                Dictionary<string, string> altTitlesDict = new();
                foreach (JsonNode? altTitleNode in altTitlesObject)
                {
                    JsonObject altTitleObject = (JsonObject)altTitleNode!;
                    string key = ((IDictionary<string, JsonNode?>)altTitleObject).Keys.ToArray()[0];
                    altTitlesDict.TryAdd(key, altTitleObject[key]!.GetValue<string>());
                }

                JsonArray tagsObject = attributes["tags"]!.AsArray();
                HashSet<string> tags = new();
                foreach (JsonNode? tagNode in tagsObject)
                {
                    JsonObject tagObject = (JsonObject)tagNode!;
                    if(tagObject["attributes"]!["name"]!.AsObject().ContainsKey("en"))
                        tags.Add(tagObject["attributes"]!["name"]!["en"]!.GetValue<string>());
                }

                string? posterId = null;
                HashSet<string> authorIds = new();
                if (manga.ContainsKey("relationships") && manga["relationships"] is not null)
                {
                    JsonArray relationships = manga["relationships"]!.AsArray();
                    posterId = relationships.FirstOrDefault(relationship => relationship!["type"]!.GetValue<string>() == "cover_art")!["id"]!.GetValue<string>();
                    foreach (JsonNode? node in relationships.Where(relationship =>
                                 relationship!["type"]!.GetValue<string>() == "author"))
                        authorIds.Add(node!["id"]!.GetValue<string>());
                }
                string? coverUrl = GetCoverUrl(publicationId, posterId);
                string? coverCacheName = null;
                if (coverUrl is not null)
                    coverCacheName = SaveCoverImageToCache(coverUrl, (byte)RequestType.AtHomeServer);

                List<string> authors = GetAuthors(authorIds);

                Dictionary<string, string> linksDict = new();
                if (attributes.ContainsKey("links") && attributes["links"] is not null)
                {
                    JsonObject linksObject = attributes["links"]!.AsObject();
                    foreach (string key in ((IDictionary<string, JsonNode?>)linksObject).Keys)
                    {
                        linksDict.Add(key, linksObject[key]!.GetValue<string>());
                    }
                }
                
                int? year = attributes.ContainsKey("year") && attributes["year"] is not null
                    ? attributes["year"]!.GetValue<int?>()
                    : null;

                string? originalLanguage = attributes.ContainsKey("originalLanguage") && attributes["originalLanguage"] is not null
                    ? attributes["originalLanguage"]!.GetValue<string?>()
                    : null;
                
                string status = attributes["status"]!.GetValue<string>();

                Publication pub = new (
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
                    status,
                    publicationId
                );
                publications.Add(pub); //Add Publication (Manga) to result
            }
        }

        cachedPublications.AddRange(publications);
        Log($"Retrieved {publications.Count} publications. Term=\"{publicationTitle}\"");
        return publications.ToArray();
    }

    public override Chapter[] GetChapters(Publication publication, string language="en")
    {
        Log($"Getting chapters {publication}");
        const int limit = 100; //How many values we want returned at once
        int offset = 0; //"Page"
        int total = int.MaxValue; //How many total results are there, is updated on first request
        List<Chapter> chapters = new();
        //As long as we haven't requested all "Pages"
        while (offset < total)
        {
            //Request next "Page"
            DownloadClient.RequestResult requestResult =
                downloadClient.MakeRequest(
                    $"https://api.mangadex.org/manga/{publication.publicationId}/feed?limit={limit}&offset={offset}&translatedLanguage%5B%5D={language}", (byte)RequestType.Feed);
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
                
                if(chapterNum is not "null")
                    chapters.Add(new Chapter(publication, title, volume, chapterNum, chapterId));
            }
        }

        //Return Chapters ordered by Chapter-Number
        NumberFormatInfo chapterNumberFormatInfo = new() { NumberDecimalSeparator = "." };
        Log($"Got {chapters.Count} chapters. {publication}");
        return chapters.OrderBy(chapter => Convert.ToSingle(chapter.chapterNumber, chapterNumberFormatInfo)).ToArray();
    }

    public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
    {
        if (progressToken?.cancellationRequested ?? false)
            return HttpStatusCode.RequestTimeout;
        Log($"Retrieving chapter-info {chapter} {chapter.parentPublication}");
        //Request URLs for Chapter-Images
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest($"https://api.mangadex.org/at-home/server/{chapter.url}?forcePort443=false'", (byte)RequestType.AtHomeServer);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return requestResult.statusCode;
        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
        if (result is null)
            return HttpStatusCode.NoContent;

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
        return DownloadChapterImages(imageUrls.ToArray(), chapter.GetArchiveFilePath(settings.downloadLocation), (byte)RequestType.AtHomeServer, comicInfoPath, progressToken:progressToken);
    }

    private string? GetCoverUrl(string publicationId, string? posterId)
    {
        Log($"Getting CoverUrl for Publication {publicationId}");
        if (posterId is null)
        {
            Log("No cover.");
            return null;
        }
        
        //Request information where to download Cover
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest($"https://api.mangadex.org/cover/{posterId}", (byte)RequestType.CoverUrl);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return null;
        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
        if (result is null)
            return null;

        string fileName = result["data"]!["attributes"]!["fileName"]!.GetValue<string>();

        string coverUrl = $"https://uploads.mangadex.org/covers/{publicationId}/{fileName}";
        Log($"Cover-Url {publicationId} -> {coverUrl}");
        return coverUrl;
    }

    private List<string> GetAuthors(IEnumerable<string> authorIds)
    {
        Log("Retrieving authors.");
        List<string> ret = new();
        foreach (string authorId in authorIds)
        {
            DownloadClient.RequestResult requestResult =
                downloadClient.MakeRequest($"https://api.mangadex.org/author/{authorId}", (byte)RequestType.Author);
            if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
                return ret;
            JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
            if (result is null)
                return ret;

            string authorName = result["data"]!["attributes"]!["name"]!.GetValue<string>();
            ret.Add(authorName);
            Log($"Got author {authorId} -> {authorName}");
        }
        return ret;
    }
}