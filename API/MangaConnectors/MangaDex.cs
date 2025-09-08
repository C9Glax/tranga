using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using Newtonsoft.Json.Linq;

namespace API.MangaConnectors;

public class MangaDex : MangaConnector
{
    //https://api.mangadex.org/docs/3-enumerations/#language-codes--localization
    //https://en.wikipedia.org/wiki/List_of_ISO_639_language_codes
    //https://gist.github.com/Josantonius/b455e315bc7f790d14b136d61d9ae469
    public MangaDex() : base("MangaDex", 
        ["en","pt","pt-br","it","de","ru","aa","ab","ae","af","ak","am","an","ar-ae","ar-bh","ar-dz","ar-eg","ar-iq","ar-jo","ar-kw","ar-lb","ar-ly","ar-ma","ar-om","ar-qa","ar-sa","ar-sy","ar-tn","ar-ye","ar","as","av","ay","az","ba","be","bg","bh","bi","bm","bn","bo","br","bs","ca","ce","ch","co","cr","cs","cu","cv","cy","da","de-at","de-ch","de-de","de-li","de-lu","div","dv","dz","ee","el","en-au","en-bz","en-ca","en-cb","en-gb","en-ie","en-jm","en-nz","en-ph","en-tt","en-us","en-za","en-zw","eo","es-ar","es-bo","es-cl","es-co","es-cr","es-do","es-ec","es-es","es-gt","es-hn","es-la","es-mx","es-ni","es-pa","es-pe","es-pr","es-py","es-sv","es-us","es-uy","es-ve","es","et","eu","fa","ff","fi","fj","fo","fr-be","fr-ca","fr-ch","fr-fr","fr-lu","fr-mc","fr","fy","ga","gd","gl","gn","gu","gv","ha","he","hi","ho","hr-ba","hr-hr","hr","ht","hu","hy","hz","ia","id","ie","ig","ii","ik","in","io","is","it-ch","it-it","iu","iw","ja","ja-ro","ji","jv","jw","ka","kg","ki","kj","kk","kl","km","kn","ko","ko-ro","kr","ks","ku","kv","kw","ky","kz","la","lb","lg","li","ln","lo","ls","lt","lu","lv","mg","mh","mi","mk","ml","mn","mo","mr","ms-bn","ms-my","ms","mt","my","na","nb","nd","ne","ng","nl-be","nl-nl","nl","nn","no","nr","ns","nv","ny","oc","oj","om","or","os","pa","pi","pl","ps","pt-pt","qu-bo","qu-ec","qu-pe","qu","rm","rn","ro","rw","sa","sb","sc","sd","se-fi","se-no","se-se","se","sg","sh","si","sk","sl","sm","sn","so","sq","sr-ba","sr-sp","sr","ss","st","su","sv-fi","sv-se","sv","sw","sx","syr","ta","te","tg","th","ti","tk","tl","tn","to","tr","ts","tt","tw","ty","ug","uk","ur","us","uz","ve","vi","vo","wa","wo","xh","yi","yo","za","zh-cn","zh-hk","zh-mo","zh-ro","zh-sg","zh-tw","zh","zu"],
        ["mangadex.org"], 
        "https://mangadex.org/favicon.ico")
    {
        this.downloadClient = new HttpDownloadClient();
    }

    private const int Limit = 100;
    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        Log.Info($"Searching Obj: {mangaSearchName}");
        List<(Manga, MangaConnectorId<Manga>)> mangas = new ();
        
        int offset = 0;
        int total = int.MaxValue;
        while(offset < total)
        {
            string requestUrl =
                $"https://api.mangadex.org/manga?limit={Limit}&offset={offset}&title={mangaSearchName}" +
                $"&contentRating%5B%5D=safe&contentRating%5B%5D=suggestive&contentRating%5B%5D=erotica" +
                $"&includes%5B%5D=manga&includes%5B%5D=cover_art&includes%5B%5D=author&includes%5B%5D=artist&includes%5B%5D=tag'";
            offset += Limit;

            RequestResult result = downloadClient.MakeRequest(requestUrl, RequestType.MangaDexFeed);
            if ((int)result.statusCode < 200 || (int)result.statusCode >= 300)
            {
                Log.Error("Request failed");
                return [];
            }

            using StreamReader sr = new (result.result);
            JObject jObject = JObject.Parse(sr.ReadToEnd());

            if (jObject.Value<string>("result") != "ok")
            {
                JArray? errors = jObject["errors"] as JArray;
                Log.Error($"Request failed: {string.Join(',', errors?.Select(e => e.Value<string>("title")) ?? [])}");
                return [];
            }

            total = jObject.Value<int>("total");
            
            JArray? data = jObject.Value<JArray>("data");
            if (data is null)
            {
                Log.Error("Data was null");
                return [];
            }
            
            mangas.AddRange(data.Select(ParseMangaFromJToken));
        }
        
        Log.Info($"Search {mangaSearchName} yielded {mangas.Count} results.");
        return mangas.ToArray();
    }

    private static readonly Regex GetMangaIdFromUrl = new(@"https?:\/\/mangadex\.org\/title\/([a-z0-9-]+)\/?.*");
    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
        Log.Info($"Getting Obj: {url}");
        if (!UrlMatchesConnector(url))
        {
            Log.Debug($"Url is not for Connector. {url}");
            return null;
        }

        Match match = GetMangaIdFromUrl.Match(url);
        if (!match.Success || !match.Groups[1].Success)
        {
            Log.Debug($"Url is not for Connector (Could not retrieve id). {url}");
            return null;
        }
        string id = match.Groups[1].Value;

        return GetMangaFromId(id);
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
        Log.Info($"Getting Obj: {mangaIdOnSite}");
        string requestUrl =
            $"https://api.mangadex.org/manga/{mangaIdOnSite}" +
            $"?includes%5B%5D=manga&includes%5B%5D=cover_art&includes%5B%5D=author&includes%5B%5D=artist&includes%5B%5D=tag'";
        
        RequestResult result = downloadClient.MakeRequest(requestUrl, RequestType.MangaDexFeed);
        if ((int)result.statusCode < 200 || (int)result.statusCode >= 300)
        {
            Log.Error("Request failed");
            return null;
        }

        using StreamReader sr = new (result.result);
        JObject jObject = JObject.Parse(sr.ReadToEnd());

        if (jObject.Value<string>("result") != "ok")
        {
            JArray? errors = jObject["errors"] as JArray;
            Log.Error($"Request failed: {string.Join(',', errors?.Select(e => e.Value<string>("title")) ?? [])}");
            return null;
        }
        
        JObject? data = jObject["data"] as JObject;
        if (data is null)
        {
            Log.Error("Data was null");
            return null;
        }

        return ParseMangaFromJToken(data);
    }

    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> manga, string? language = null)
    {
        Log.Info($"Getting Chapters: {manga.IdOnConnectorSite}");
        List<(Chapter, MangaConnectorId<Chapter>)> chapters = new ();
        
        int offset = 0;
        int total = int.MaxValue;
        while(offset < total)
        {
            string requestUrl =
                $"https://api.mangadex.org/manga/{manga.IdOnConnectorSite}/feed?limit={Limit}&offset={offset}&" +
                $"translatedLanguage%5B%5D={language}&" +
                $"contentRating%5B%5D=safe&contentRating%5B%5D=suggestive&contentRating%5B%5D=erotica&includeFutureUpdates=0&includes%5B%5D=";
            offset += Limit;

            RequestResult result = downloadClient.MakeRequest(requestUrl, RequestType.MangaDexFeed);
            if ((int)result.statusCode < 200 || (int)result.statusCode >= 300)
            {
                Log.Error("Request failed");
                return [];
            }

            using StreamReader sr = new (result.result);
            JObject jObject = JObject.Parse(sr.ReadToEnd());

            if (jObject.Value<string>("result") != "ok")
            {
                JArray? errors = jObject["errors"] as JArray;
                Log.Error($"Request failed: {string.Join(',', errors?.Select(e => e.Value<string>("title")) ?? [])}");
                return [];
            }

            total = jObject.Value<int>("total");
            
            JArray? data = jObject.Value<JArray>("data");
            if (data is null)
            {
                Log.Error("Data was null");
                return [];
            }
            
            chapters.AddRange(data.Select(d => ParseChapterFromJToken(manga, d)));
        }
        
        Log.Info($"Request for chapters for {manga.Obj.Name} yielded {chapters.Count} results.");
        return chapters.ToArray();
    }

    private static readonly Regex GetChapterIdFromUrl = new(@"https?:\/\/mangadex\.org\/chapter\/([a-z0-9-]+)\/?.*");
    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        Log.Info($"Getting Chapter Image-Urls: {chapterId.Obj}");
        if (chapterId.WebsiteUrl is null || !UrlMatchesConnector(chapterId.WebsiteUrl))
        {
            Log.Debug($"Url is not for Connector. {chapterId.WebsiteUrl}");
            return [];
        }

        Match match = GetChapterIdFromUrl.Match(chapterId.WebsiteUrl);
        if (!match.Success || !match.Groups[1].Success)
        {
            Log.Debug($"Url is not for Connector (Could not retrieve id). {chapterId.WebsiteUrl}");
            return [];
        }
        
        string id = match.Groups[1].Value;
        string requestUrl = $"https://api.mangadex.org/at-home/server/{id}";
        
        RequestResult result = downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)result.statusCode < 200 || (int)result.statusCode >= 300)
        {
            Log.Error("Request failed");
            return [];
        }

        using StreamReader sr = new (result.result);
        JObject jObject = JObject.Parse(sr.ReadToEnd());
        
        if (jObject.Value<string>("result") != "ok")
        {
            JArray? errors = jObject["errors"] as JArray;
            Log.Error($"Request failed: {string.Join(',', errors?.Select(e => e.Value<string>("title")) ?? [])}");
            return [];
        }
        
        string? baseUrl = jObject.Value<string>("baseUrl");
        JToken? chapterToken = jObject["chapter"];
        string? hash = chapterToken?.Value<string>("hash");
        JArray? data = chapterToken?["data"] as JArray;

        if (baseUrl is null || hash is null || data is null)
        {
            Log.Error("Data was null");
            return [];
        }

        IEnumerable<string> urls = data.Select(t => $"{baseUrl}/data/{hash}/{t.Value<string>()}");
        
        return urls.ToArray();
    }

    private (Manga manga, MangaConnectorId<Manga> id) ParseMangaFromJToken(JToken jToken)
    {
        string? id = jToken.Value<string>("id");
        if(id is null)
            throw new Exception("jToken was not in expected format");
        
        JObject? attributes = jToken["attributes"] as JObject;
        if(attributes is null)
            throw new Exception("jToken was not in expected format");
        string? name = attributes["title"]?.Value<string>("en") ?? attributes["title"]?.First?.First?.Value<string>();
        string description = attributes["description"]?.Value<string>("en")??attributes["description"]?.First?.First?.Value<string>()??"";
        string? status = attributes["status"]?.Value<string>();
        uint? year = attributes["year"]?.Value<uint?>();
        string? originalLanguage = attributes["originalLanguage"]?.Value<string>();
        JArray? altTitlesJArray = attributes.TryGetValue("altTitles", out JToken? altTitlesArray) ? altTitlesArray as JArray : null;
        JArray? tagsJArray = attributes.TryGetValue("tags", out JToken? tagsArray) ? tagsArray as JArray : null;
        JArray? relationships = jToken["relationships"] as JArray;
        if (name is null || status is null || relationships is null)
            throw new Exception("jToken was not in expected format");
        
        string? coverFileName = relationships.FirstOrDefault(r => r["type"]?.Value<string>() == "cover_art")?["attributes"]?.Value<string>("fileName");
        if(coverFileName is null)
            throw new Exception("jToken was not in expected format");
        
        List<Link> links = attributes["links"]?
            .ToObject<Dictionary<string,string>>()?
            .Select(kv =>
            {
                //https://api.mangadex.org/docs/3-enumerations/#manga-links-data
                string url = kv.Key switch
                {
                    "al" => $"https://anilist.co/manga/{kv.Value}",
                    "ap" => $"https://www.anime-planet.com/manga/{kv.Value}",
                    "bw" => $"https://bookwalker.jp/{kv.Value}",
                    "mu" => $"https://www.mangaupdates.com/series.html?id={kv.Value}",
                    "nu" => $"https://www.novelupdates.com/series/{kv.Value}",
                    "mal" => $"https://myanimelist.net/manga/{kv.Value}",
                    _ => kv.Value
                };
                string key = kv.Key switch
                {
                    "al" => "AniList",
                    "ap" => "Anime Planet",
                    "bw" => "BookWalker",
                    "mu" => "Obj Updates",
                    "nu" => "Novel Updates",
                    "kt" => "Kitsu.io",
                    "amz" => "Amazon",
                    "ebj" => "eBookJapan",
                    "mal" => "MyAnimeList",
                    "cdj" => "CDJapan",
                    _ => kv.Key
                };
                return new Link(key, url);
            }).ToList()??[];

        List<AltTitle> altTitles = altTitlesJArray?
            .Select(t =>
            {
                JObject? j = t as JObject;
                JProperty? p = j?.Properties().First();
                if (p is null)
                    return null;
                return new AltTitle(p.Name, p.Value.ToString());
            }).Where(x => x is not null).Cast<AltTitle>().ToList()??[];
        
        List<MangaTag> tags = tagsJArray?
            .Where(t => t.Value<string>("type") == "tag")
            .Select(t => t["attributes"]?["name"]?.Value<string>("en")??t["attributes"]?["name"]?.First?.First?.Value<string>())
            .Select(str => str is not null ? new MangaTag(str) : null)
            .Where(x => x is not null).Cast<MangaTag>().ToList()??[];
        
        List<Author> authors = relationships
            .Where(r => r["type"]?.Value<string>() == "author")
            .Select(t => t["attributes"]?.Value<string>("name"))
            .Select(str => str is not null ? new Author(str) : null)
            .Where(x => x is not null).Cast<Author>().ToList();
            
        
        MangaReleaseStatus releaseStatus = status switch
        {
            "completed" => MangaReleaseStatus.Completed,
            "ongoing" => MangaReleaseStatus.Continuing,
            "cancelled" => MangaReleaseStatus.Cancelled,
            "hiatus" => MangaReleaseStatus.OnHiatus,
            _ => MangaReleaseStatus.Unreleased
        };
        string websiteUrl = $"https://mangadex.org/title/{id}";
        string coverUrl = $"https://uploads.mangadex.org/covers/{id}/{coverFileName}";

        Manga manga = new (name, description, coverUrl, releaseStatus, authors, tags, links,altTitles,
            null, 0f, year, originalLanguage);
        MangaConnectorId<Manga> mcId = new (manga, this, id, websiteUrl);
        manga.MangaConnectorIds.Add(mcId);
        return (manga, mcId);
    }

    private (Chapter chapter, MangaConnectorId<Chapter> id) ParseChapterFromJToken(MangaConnectorId<Manga> mcIdManga, JToken jToken)
    {
        string? id = jToken.Value<string>("id");
        JToken? attributes = jToken["attributes"];
        string? chapterStr = attributes?.Value<string>("chapter") ?? "0";
        string? volumeStr = attributes?.Value<string>("volume");
        int? volumeNumber = null;
        string? title = attributes?.Value<string>("title");
        
        if(id is null || chapterStr is null)
            throw new Exception("jToken was not in expected format");
        if(volumeStr is not null)
            volumeNumber = int.Parse(volumeStr);
        
        string websiteUrl = $"https://mangadex.org/chapter/{id}";
        Chapter chapter = new (mcIdManga.Obj, chapterStr, volumeNumber, title);
        MangaConnectorId<Chapter> mcId = new(chapter, this, id, websiteUrl);
        chapter.MangaConnectorIds.Add(mcId);
        return (chapter, mcId);
    }
}