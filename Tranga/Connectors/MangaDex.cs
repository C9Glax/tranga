﻿using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Logging;

namespace Tranga.Connectors;
public class MangaDex : Connector
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

    public MangaDex(string downloadLocation, string imageCachePath, Logger? logger) : base(downloadLocation, imageCachePath, logger)
    {
        name = "MangaDex";
        this.downloadClient = new DownloadClient(new Dictionary<byte, int>()
        {
            {(byte)RequestType.Manga, 250},
            {(byte)RequestType.Feed, 250},
            {(byte)RequestType.AtHomeServer, 40},
            {(byte)RequestType.CoverUrl, 250},
            {(byte)RequestType.Author, 250}
        }, logger);
    }

    public override Publication[] GetPublications(string publicationTitle = "")
    {
        logger?.WriteLine(this.GetType().ToString(), $"Getting Publications (title={publicationTitle})");
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
            if (requestResult.statusCode != HttpStatusCode.OK)
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
                logger?.WriteLine(this.GetType().ToString(), $"Getting publication data. {++loadedPublicationData}/{total}");
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
                string? authorId = null;
                if (manga.ContainsKey("relationships") && manga["relationships"] is not null)
                {
                    JsonArray relationships = manga["relationships"]!.AsArray();
                    posterId = relationships.FirstOrDefault(relationship => relationship!["type"]!.GetValue<string>() == "cover_art")!["id"]!.GetValue<string>();
                    authorId = relationships.FirstOrDefault(relationship => relationship!["type"]!.GetValue<string>() == "author")!["id"]!.GetValue<string>();
                }
                string? coverUrl = GetCoverUrl(publicationId, posterId);
                string? coverCacheName = null;
                if (coverUrl is not null)
                    coverCacheName = SaveCoverImageToCache(coverUrl, (byte)RequestType.AtHomeServer);
                
                string? author = GetAuthor(authorId);

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
                    author,
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

        logger?.WriteLine(this.GetType().ToString(), $"Done getting publications (title={publicationTitle})");
        return publications.ToArray();
    }

    public override Chapter[] GetChapters(Publication publication, string language = "")
    {
        logger?.WriteLine(this.GetType().ToString(), $"Getting Chapters for {publication.sortName} {publication.internalId} (language={language})");
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
            if (requestResult.statusCode != HttpStatusCode.OK)
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
                
                string? chapterNum = attributes.ContainsKey("chapter") && attributes["chapter"] is not null
                    ? attributes["chapter"]!.GetValue<string>()
                    : null;
                
                chapters.Add(new Chapter(title, volume, chapterNum, chapterId));
            }
        }

        //Return Chapters ordered by Chapter-Number
        NumberFormatInfo chapterNumberFormatInfo = new()
        {
            NumberDecimalSeparator = "."
        };
        logger?.WriteLine(this.GetType().ToString(), $"Done getting Chapters for {publication.internalId}");
        return chapters.OrderBy(chapter => Convert.ToSingle(chapter.chapterNumber, chapterNumberFormatInfo)).ToArray();
    }

    public override void DownloadChapter(Publication publication, Chapter chapter, TrangaTask parentTask)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Downloading Chapter-Info {publication.sortName} {publication.internalId} {chapter.volumeNumber}-{chapter.chapterNumber}");
        //Request URLs for Chapter-Images
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest($"https://api.mangadex.org/at-home/server/{chapter.url}?forcePort443=false'", (byte)RequestType.AtHomeServer);
        if (requestResult.statusCode != HttpStatusCode.OK)
            return;
        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
        if (result is null)
            return;

        string baseUrl = result["baseUrl"]!.GetValue<string>();
        string hash = result["chapter"]!["hash"]!.GetValue<string>();
        JsonArray imageFileNames = result["chapter"]!["data"]!.AsArray();
        //Loop through all imageNames and construct urls (imageUrl)
        HashSet<string> imageUrls = new();
        foreach (JsonNode? image in imageFileNames)
            imageUrls.Add($"{baseUrl}/data/{hash}/{image!.GetValue<string>()}");

        string comicInfoPath = Path.GetTempFileName();
        File.WriteAllText(comicInfoPath, GetComicInfoXmlString(publication, chapter, logger));
        
        //Download Chapter-Images
        DownloadChapterImages(imageUrls.ToArray(), GetArchiveFilePath(publication, chapter), (byte)RequestType.AtHomeServer, parentTask, comicInfoPath);
    }

    private string? GetCoverUrl(string publicationId, string? posterId)
    {
        logger?.WriteLine(this.GetType().ToString(), $"Getting CoverUrl for {publicationId}");
        if (posterId is null)
        {
            logger?.WriteLine(this.GetType().ToString(), $"No posterId, aborting");
            return null;
        }
        
        //Request information where to download Cover
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest($"https://api.mangadex.org/cover/{posterId}", (byte)RequestType.CoverUrl);
        if (requestResult.statusCode != HttpStatusCode.OK)
            return null;
        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
        if (result is null)
            return null;

        string fileName = result["data"]!["attributes"]!["fileName"]!.GetValue<string>();

        string coverUrl = $"https://uploads.mangadex.org/covers/{publicationId}/{fileName}";
        logger?.WriteLine(this.GetType().ToString(), $"Got Cover-Url for {publicationId} -> {coverUrl}");
        return coverUrl;
    }

    private string? GetAuthor(string? authorId)
    {
        if (authorId is null)
            return null;
        
        DownloadClient.RequestResult requestResult =
            downloadClient.MakeRequest($"https://api.mangadex.org/author/{authorId}", (byte)RequestType.Author);
        if (requestResult.statusCode != HttpStatusCode.OK)
            return null;
        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
        if (result is null)
            return null;

        string author = result["data"]!["attributes"]!["name"]!.GetValue<string>();
        logger?.WriteLine(this.GetType().ToString(), $"Got author {authorId} -> {author}");
        return author;
    }
}