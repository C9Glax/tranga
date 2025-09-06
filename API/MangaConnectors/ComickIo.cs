using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using Newtonsoft.Json.Linq;

namespace API.MangaConnectors;

public class ComickIo : MangaConnector
{
    //https://api.comick.io/docs/
    //https://en.wikipedia.org/wiki/List_of_ISO_639_language_codes
    
    public ComickIo() : base("ComickIo", 
        ["en","pt","pt-br","it","de","ru","aa","ab","ae","af","ak","am","an","ar-ae","ar-bh","ar-dz","ar-eg","ar-iq","ar-jo","ar-kw","ar-lb","ar-ly","ar-ma","ar-om","ar-qa","ar-sa","ar-sy","ar-tn","ar-ye","ar","as","av","ay","az","ba","be","bg","bh","bi","bm","bn","bo","br","bs","ca","ce","ch","co","cr","cs","cu","cv","cy","da","de-at","de-ch","de-de","de-li","de-lu","div","dv","dz","ee","el","en-au","en-bz","en-ca","en-cb","en-gb","en-ie","en-jm","en-nz","en-ph","en-tt","en-us","en-za","en-zw","eo","es-ar","es-bo","es-cl","es-co","es-cr","es-do","es-ec","es-es","es-gt","es-hn","es-la","es-mx","es-ni","es-pa","es-pe","es-pr","es-py","es-sv","es-us","es-uy","es-ve","es","et","eu","fa","ff","fi","fj","fo","fr-be","fr-ca","fr-ch","fr-fr","fr-lu","fr-mc","fr","fy","ga","gd","gl","gn","gu","gv","ha","he","hi","ho","hr-ba","hr-hr","hr","ht","hu","hy","hz","ia","id","ie","ig","ii","ik","in","io","is","it-ch","it-it","iu","iw","ja","ja-ro","ji","jv","jw","ka","kg","ki","kj","kk","kl","km","kn","ko","ko-ro","kr","ks","ku","kv","kw","ky","kz","la","lb","lg","li","ln","lo","ls","lt","lu","lv","mg","mh","mi","mk","ml","mn","mo","mr","ms-bn","ms-my","ms","mt","my","na","nb","nd","ne","ng","nl-be","nl-nl","nl","nn","no","nr","ns","nv","ny","oc","oj","om","or","os","pa","pi","pl","ps","pt-pt","qu-bo","qu-ec","qu-pe","qu","rm","rn","ro","rw","sa","sb","sc","sd","se-fi","se-no","se-se","se","sg","sh","si","sk","sl","sm","sn","so","sq","sr-ba","sr-sp","sr","ss","st","su","sv-fi","sv-se","sv","sw","sx","syr","ta","te","tg","th","ti","tk","tl","tn","to","tr","ts","tt","tw","ty","ug","uk","ur","us","uz","ve","vi","vo","wa","wo","xh","yi","yo","za","zh-cn","zh-hk","zh-mo","zh-ro","zh-sg","zh-tw","zh","zu"],
        ["comick.io"], 
        "https://comick.io/static/icons/unicorn-64.png")
    {
        this.downloadClient = new HttpDownloadClient();
    }

    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        Log.Info($"Searching Obj: {mangaSearchName}");

        List<string> slugs = new();
        int page = 1;
        while(page < 50)
        {
            string requestUrl = $"https://api.comick.fun/v1.0/search/?type=comic&t=false&limit=100&showall=true&" +
                                $"page={page}&q={mangaSearchName}";

            RequestResult result = downloadClient.MakeRequest(requestUrl, RequestType.Default);
            if ((int)result.statusCode < 200 || (int)result.statusCode >= 300)
            {
                Log.Error("Request failed");
                return [];
            }

            using StreamReader sr = new (result.result);
            JArray data = JArray.Parse(sr.ReadToEnd());

            if (data.Count < 1)
                break;
            
            slugs.AddRange(data.Select(token => token.Value<string>("slug")!));
            page++;
        }
        Log.Debug($"Search {mangaSearchName} yielded {slugs.Count} slugs. Requesting mangas now...");


        List<(Manga, MangaConnectorId<Manga>)> mangas = new ();
        foreach (string slug in slugs)
        {
            if(GetMangaFromId(slug) is { } entry)
                mangas.Add(entry);
        }
        
        Log.Info($"Search {mangaSearchName} yielded {mangas.Count} results.");
        return mangas.ToArray();
    }

    private readonly Regex _getSlugFromTitleRex = new(@"https?:\/\/comick\.io\/comic\/(.+)(?:\/.*)*");
    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
        Match m = _getSlugFromTitleRex.Match(url);
        return m.Groups[1].Success ? GetMangaFromId(m.Groups[1].Value) : null;
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
        string requestUrl = $"https://api.comick.fun/comic/{mangaIdOnSite}";

        RequestResult result = downloadClient.MakeRequest(requestUrl, RequestType.MangaInfo);
        if ((int)result.statusCode < 200 || (int)result.statusCode >= 300)
        {
            Log.Error("Request failed");
            return null;
        }
        using StreamReader sr = new (result.result);
        JToken data = JToken.Parse(sr.ReadToEnd());

        return ParseMangaFromJToken(data);
    }

    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> mangaConnectorId,
        string? language = null)
    {
        Log.Info($"Getting Chapters: {mangaConnectorId.IdOnConnectorSite}");
        List<(Chapter, MangaConnectorId<Chapter>)> chapters = new();
        int page = 1;
        while(page < 50)
        {
            string requestUrl = $"https://api.comick.fun/comic/{mangaConnectorId.IdOnConnectorSite}/chapters?limit=100&page={page}&lang={language}";

            RequestResult result = downloadClient.MakeRequest(requestUrl, RequestType.MangaInfo);
            if ((int)result.statusCode < 200 || (int)result.statusCode >= 300)
            {
                Log.Error("Request failed");
                return [];
            }

            using StreamReader sr = new (result.result);
            JToken data = JToken.Parse(sr.ReadToEnd());
            JArray? chaptersArray = data["chapters"] as JArray;

            if (chaptersArray is null || chaptersArray.Count < 1)
                break;
            
            chapters.AddRange(ParseChapters(mangaConnectorId, chaptersArray));

            page++;
        }

        return chapters.ToArray();
    }

    private readonly Regex _hidFromUrl = new(@"https?:\/\/comick\.io\/comic\/.+\/([^-]+).*");
    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        
        Log.Info($"Getting Chapter Image-Urls: {chapterId.Obj}");
        if (chapterId.WebsiteUrl is null || !UrlMatchesConnector(chapterId.WebsiteUrl))
        {
            Log.Debug($"Url is not for Connector. {chapterId.WebsiteUrl}");
            return [];
        }
        
        Match m = _hidFromUrl.Match(chapterId.WebsiteUrl);
        if (!m.Groups[1].Success)
        {
            Log.Debug($"Could not parse hid from url. {chapterId.WebsiteUrl}");
            return [];
        }
        
        string hid = m.Groups[1].Value;
        
        string requestUrl = $"https://api.comick.fun/chapter/{hid}/get_images";
        RequestResult result = downloadClient.MakeRequest(requestUrl, RequestType.MangaInfo);
        if ((int)result.statusCode < 200 || (int)result.statusCode >= 300)
        {
            Log.Error("Request failed");
            return [];
        }
        
        using StreamReader sr = new (result.result);
        JArray data = JArray.Parse(sr.ReadToEnd());

        return data.Select(token =>
        {
            string url = $"https://meo.comick.pictures/{token.Value<string>("b2key")}";
            return url;
        }).ToArray();
    }

    private (Manga manga, MangaConnectorId<Manga> id) ParseMangaFromJToken(JToken json)
    {
        string? hid = json["comic"]?.Value<string>("hid");
        string? slug = json["comic"]?.Value<string>("slug");
        string? name = json["comic"]?.Value<string>("title");
        string? description = json["comic"]?.Value<string>("desc");
        string? originalLanguage = json["comic"]?.Value<string>("country");
        string url = $"https://comick.io/comic/{slug}";
        string? coverName = json["comic"]?["md_covers"]?.First?.Value<string>("b2key");
        string coverUrl = $"https://meo.comick.pictures/{coverName}";
        int? releaseStatusStr = json["comic"]?.Value<int>("status");
        MangaReleaseStatus status = releaseStatusStr switch
        {
            1 => MangaReleaseStatus.Continuing,
            2 => MangaReleaseStatus.Completed,
            3 => MangaReleaseStatus.Cancelled,
            4 => MangaReleaseStatus.OnHiatus,
            _ => MangaReleaseStatus.Unreleased
        };
        uint? year = json["comic"]?.Value<uint?>("year");
        JArray? altTitlesArray = json["comic"]?["md_titles"] as JArray;
        //Cant let language be null, so fill with whatever.
        byte whatever = 0;
        List<AltTitle> altTitles = altTitlesArray?
            .Select(token => new AltTitle(token.Value<string>("lang")??whatever++.ToString(), token.Value<string>("title")!))
            .ToList()??[];
        
        JArray? authorsArray = json["authors"] as JArray;
        JArray? artistsArray = json["artists"] as JArray;
        List<Author> authors = authorsArray?.Concat(artistsArray!)
            .Select(token => new Author(token.Value<string>("name")!))
            .DistinctBy(a => a.Key)
            .ToList()??[];
        
        JArray? genreArray = json["comic"]?["md_comic_md_genres"] as JArray;
        List<MangaTag> tags = genreArray?
            .Select(token => new MangaTag(token["md_genres"]?.Value<string>("name")!))
            .ToList()??[];
        
        JArray? linksArray = json["comic"]?["links"] as JArray;
        List<Link> links = linksArray?
            .ToObject<Dictionary<string,string>>()?
            .Select(kv =>
            {
                string fullUrl = kv.Key switch
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
                return new Link(key, fullUrl);
            }).ToList()??[];
        
        if(hid is null)
            throw new Exception("hid is null");
        if(slug is null)
            throw new Exception("slug is null");
        if(name is null)
            throw new Exception("name is null");
        
        Manga manga = new (name, description??"", coverUrl, status, authors, tags, links, altTitles,
            year: year, originalLanguage: originalLanguage);
        MangaConnectorId<Manga> mcId = new (manga, this, hid, url);
        manga.MangaConnectorIds.Add(mcId);
        return (manga, mcId);
    }

    private List<(Chapter, MangaConnectorId<Chapter>)> ParseChapters(MangaConnectorId<Manga> mcIdManga, JArray chaptersArray)
    {
        List<(Chapter, MangaConnectorId<Chapter>)> chapters = new ();
        foreach (JToken chapter in chaptersArray)
        {
            string? chapterNum = chapter.Value<string>("chap");
            string? volumeNumStr = chapter.Value<string>("vol");
            int? volumeNum = volumeNumStr is null ? null : int.Parse(volumeNumStr);
            string? title = chapter.Value<string>("title");
            string? hid = chapter.Value<string>("hid");
            string url = $"https://comick.io/comic/{mcIdManga.IdOnConnectorSite}/{hid}";
            
            if(chapterNum is null || hid is null)
                continue;
            
            Chapter ch = new (mcIdManga.Obj, chapterNum, volumeNum, title);
            MangaConnectorId<Chapter> mcId = new(ch, this, hid, url);
            ch.MangaConnectorIds.Add(mcId);
            chapters.Add((ch, mcId));
        }
        return chapters;
    }
}