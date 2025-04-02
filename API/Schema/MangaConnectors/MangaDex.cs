using System.Net;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace API.Schema.MangaConnectors;

public class MangaDex : MangaConnector
{
    //https://api.mangadex.org/docs/3-enumerations/#language-codes--localization
    //https://en.wikipedia.org/wiki/List_of_ISO_639_language_codes
    //https://gist.github.com/Josantonius/b455e315bc7f790d14b136d61d9ae469
    public MangaDex() : base("MangaDex", ["en","pt","pt-br","it","de","ru","aa","ab","ae","af","ak","am","an","ar-ae","ar-bh","ar-dz","ar-eg","ar-iq","ar-jo","ar-kw","ar-lb","ar-ly","ar-ma","ar-om","ar-qa","ar-sa","ar-sy","ar-tn","ar-ye","ar","as","av","ay","az","ba","be","bg","bh","bi","bm","bn","bo","br","bs","ca","ce","ch","co","cr","cs","cu","cv","cy","da","de-at","de-ch","de-de","de-li","de-lu","div","dv","dz","ee","el","en-au","en-bz","en-ca","en-cb","en-gb","en-ie","en-jm","en-nz","en-ph","en-tt","en-us","en-za","en-zw","eo","es-ar","es-bo","es-cl","es-co","es-cr","es-do","es-ec","es-es","es-gt","es-hn","es-la","es-mx","es-ni","es-pa","es-pe","es-pr","es-py","es-sv","es-us","es-uy","es-ve","es","et","eu","fa","ff","fi","fj","fo","fr-be","fr-ca","fr-ch","fr-fr","fr-lu","fr-mc","fr","fy","ga","gd","gl","gn","gu","gv","ha","he","hi","ho","hr-ba","hr-hr","hr","ht","hu","hy","hz","ia","id","ie","ig","ii","ik","in","io","is","it-ch","it-it","iu","iw","ja","ja-ro","ji","jv","jw","ka","kg","ki","kj","kk","kl","km","kn","ko","ko-ro","kr","ks","ku","kv","kw","ky","kz","la","lb","lg","li","ln","lo","ls","lt","lu","lv","mg","mh","mi","mk","ml","mn","mo","mr","ms-bn","ms-my","ms","mt","my","na","nb","nd","ne","ng","nl-be","nl-nl","nl","nn","no","nr","ns","nv","ny","oc","oj","om","or","os","pa","pi","pl","ps","pt-pt","qu-bo","qu-ec","qu-pe","qu","rm","rn","ro","rw","sa","sb","sc","sd","se-fi","se-no","se-se","se","sg","sh","si","sk","sl","sm","sn","so","sq","sr-ba","sr-sp","sr","ss","st","su","sv-fi","sv-se","sv","sw","sx","syr","ta","te","tg","th","ti","tk","tl","tn","to","tr","ts","tt","tw","ty","ug","uk","ur","us","uz","ve","vi","vo","wa","wo","xh","yi","yo","za","zh-cn","zh-hk","zh-mo","zh-ro","zh-sg","zh-tw","zh","zu"], ["mangadex.org"], "https://mangadex.org/favicon.ico")
    {
        this.downloadClient = new HttpDownloadClient();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(string publicationTitle = "")
    {
        const int limit = 100; //How many values we want returned at once
        int offset = 0; //"Page"
        int total = int.MaxValue; //How many total results are there, is updated on first request
        HashSet<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)> retManga = new();
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
            if(MangaFromJsonObject(mangaNode.AsObject()) is { } manga)
                retManga.Add(manga); //Add Publication (Manga) to result
        }
        return retManga.ToArray();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromId(string publicationId)
    {
        RequestResult requestResult =
            downloadClient.MakeRequest($"https://api.mangadex.org/manga/{publicationId}?includes%5B%5D=manga&includes%5B%5D=cover_art&includes%5B%5D=author&includes%5B%5D=artist&includes%5B%5D=tag", RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return null;
        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
        if(result is not null)
            return MangaFromJsonObject(result["data"]!.AsObject());
        return null;
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromUrl(string url)
    {
        Regex idRex = new (@"https:\/\/mangadex.org\/title\/([A-z0-9-]*)\/.*");
        string id = idRex.Match(url).Groups[1].Value;
        return GetMangaFromId(id);
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? MangaFromJsonObject(JsonObject manga)
    {
        if (!manga.TryGetPropertyValue("id", out JsonNode? idNode))
            return null;
        string publicationId = idNode!.GetValue<string>();
            
        if (!manga.TryGetPropertyValue("attributes", out JsonNode? attributesNode))
            return null;
        JsonObject attributes = attributesNode!.AsObject();

        if (!attributes.TryGetPropertyValue("title", out JsonNode? titleNode))
            return null;
        string sortName = titleNode!.AsObject().ContainsKey("en") switch
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
        List<MangaAltTitle> altTitles = altTitlesDict.Select(t => new MangaAltTitle(t.Key, t.Value)).ToList();

        if (!attributes.TryGetPropertyValue("description", out JsonNode? descriptionNode))
            return null;
        string description = descriptionNode!.AsObject().ContainsKey("en") switch
        {
            true => descriptionNode.AsObject()["en"]!.GetValue<string>(),
            false => descriptionNode.AsObject().FirstOrDefault().Value?.GetValue<string>() ?? ""
        };

        Dictionary<string, string> linksDict = new();
        if (attributes.TryGetPropertyValue("links", out JsonNode? linksNode) && linksNode is not null)
            foreach (KeyValuePair<string, JsonNode?> linkKv in linksNode!.AsObject())
                linksDict.TryAdd(linkKv.Key, linkKv.Value.GetValue<string>());
        List<Link> links = linksDict.Select(x => new Link(x.Key, x.Value)).ToList();

        string? originalLanguage =
            attributes.TryGetPropertyValue("originalLanguage", out JsonNode? originalLanguageNode) switch
            {
                true => originalLanguageNode?.GetValue<string>(),
                false => null
            };
        
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;
        if (attributes.TryGetPropertyValue("status", out JsonNode? statusNode))
        {
            releaseStatus = statusNode?.GetValue<string>().ToLower() switch
            {
                "ongoing" => MangaReleaseStatus.Continuing,
                "completed" => MangaReleaseStatus.Completed,
                "hiatus" => MangaReleaseStatus.OnHiatus,
                "cancelled" => MangaReleaseStatus.Cancelled,
                _ => MangaReleaseStatus.Unreleased
            };
        }

        uint year = attributes.TryGetPropertyValue("year", out JsonNode? yearNode) switch
        {
            true => yearNode?.GetValue<uint>()??0,
            false => 0
        };
        
        HashSet<string> tags = new(128);
        if (attributes.TryGetPropertyValue("tags", out JsonNode? tagsNode))
            foreach (JsonNode? tagNode in tagsNode!.AsArray())
                tags.Add(tagNode!["attributes"]!["name"]!["en"]!.GetValue<string>());
        List<MangaTag> mangaTags = tags.Select(t => new MangaTag(t)).ToList();
        
        if (!manga.TryGetPropertyValue("relationships", out JsonNode? relationshipsNode))
            return null;
        
        JsonNode? coverNode = relationshipsNode!.AsArray()
            .FirstOrDefault(rel => rel!["type"]!.GetValue<string>().Equals("cover_art"));
        if (coverNode is null)
            return null;
        string fileName = coverNode["attributes"]!["fileName"]!.GetValue<string>();
        string coverUrl = $"https://uploads.mangadex.org/covers/{publicationId}/{fileName}";
        
        List<string> authorNames = new();
        JsonNode?[] authorNodes = relationshipsNode.AsArray()
            .Where(rel => rel!["type"]!.GetValue<string>().Equals("author") || rel!["type"]!.GetValue<string>().Equals("artist")).ToArray();
        foreach (JsonNode? authorNode in authorNodes)
        {
            string authorName = authorNode!["attributes"]!["name"]!.GetValue<string>();
            if(!authorNames.Contains(authorName))
               authorNames.Add(authorName);
        }
        List<Author> authors = authorNames.Select(a => new Author(a)).ToList();

        Manga pub = new (publicationId, sortName, description, $"https://mangadex.org/title/{publicationId}", coverUrl, null, year,
            originalLanguage, releaseStatus, -1,
            this, 
            authors, 
            mangaTags, 
            links,
            altTitles);
		
        return (pub, authors, mangaTags, links, altTitles);
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
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
                    $"https://api.mangadex.org/manga/{manga.IdOnConnectorSite}/feed?limit={limit}&offset={offset}&translatedLanguage%5B%5D={language}&contentRating%5B%5D=safe&contentRating%5B%5D=suggestive&contentRating%5B%5D=erotica&contentRating%5B%5D=pornographic", RequestType.MangaDexFeed);
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
                string url = $"https://mangadex.org/chapter/{chapterId}";
                
                string? title = attributes.ContainsKey("title") && attributes["title"] is not null
                    ? attributes["title"]!.GetValue<string>()
                    : null;
                
                int? volume = attributes.ContainsKey("volume") && attributes["volume"] is not null
                    ? int.Parse(attributes["volume"]!.GetValue<string>())
                    : null;
                
                string? chapterNumStr = attributes.ContainsKey("chapter") && attributes["chapter"] is not null
                    ? attributes["chapter"]!.GetValue<string>()
                    : null;
                
                string chapterNumber = new(chapterNumStr);
                
                
                if (attributes.ContainsKey("pages") && attributes["pages"] is not null &&
                    attributes["pages"]!.GetValue<int>() < 1)
                {
                    continue;
                }
                
                try
                {
                    Chapter newChapter = new(manga, url, chapterNumber, volume, title);
                    if(!chapters.Contains(newChapter))
                        chapters.Add(newChapter);
                }
                catch (Exception e)
                {
                }
            }
        }

        //Return Chapters ordered by Chapter-Number
        return chapters.ToArray();
    }

    internal override string[] GetChapterImageUrls(Chapter chapter)
    {//Request URLs for Chapter-Images
        Match m = Regex.Match(chapter.Url, @"https?:\/\/mangadex.org\/chapter\/([0-9\-a-z]+)");
        if (!m.Success)
            return [];
        RequestResult requestResult =
            downloadClient.MakeRequest($"https://api.mangadex.org/at-home/server/{m.Groups[1].Value}?forcePort443=false", RequestType.MangaDexImage);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            return [];
        }
        JsonObject? result = JsonSerializer.Deserialize<JsonObject>(requestResult.result);
        if (result is null)
        {
            return [];
        }
        string baseUrl = result["baseUrl"]!.GetValue<string>();
        string hash = result["chapter"]!["hash"]!.GetValue<string>();
        JsonArray imageFileNames = result["chapter"]!["data"]!.AsArray();
        //Loop through all imageNames and construct urls (imageUrl)
        List<string> imageUrls = new();
        foreach (JsonNode? image in imageFileNames)
            imageUrls.Add($"{baseUrl}/data/{hash}/{image!.GetValue<string>()}");
        return imageUrls.ToArray();
    }
}