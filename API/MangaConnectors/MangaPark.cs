using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using HtmlAgilityPack;
using static System.Text.RegularExpressions.Regex;

namespace API.MangaConnectors;

public class MangaPark : MangaConnector
{
    public MangaPark() : base("MangaPark", 
        ["en"],
        ["mangapark.com", "mangapark.net", "mangapark.org", "mangapark.me", "mangapark.io", "mangapark.to", "comicpark.org", "comicpark.to", "readpark.org", "readpark.net", "parkmanga.com", "parkmanga.net", "parkmanga.org", "mpark.to"], 
        "/blahaj.png")
    {
        this.downloadClient = new HttpDownloadClient();
    }

    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        foreach (string uri in BaseUris)
            if (SearchMangaWithDomain(mangaSearchName, uri) is { } result)
                return result;
        return [];
    }

    private (Manga, MangaConnectorId<Manga>)[]? SearchMangaWithDomain(string mangaSearchName, string domain)
    {
        Uri baseUri = new($"https://{domain}/");
        
        List<(Manga, MangaConnectorId<Manga>)> ret = [];
        
        for (int page = 1;; page++) // break; in loop
        {
            Uri searchUri = new(baseUri, $"search?word={HttpUtility.UrlEncode(mangaSearchName)}&lang={Tranga.Settings.DownloadLanguage}&page={page}");
            if (downloadClient.MakeRequest(searchUri.ToString(), RequestType.Default) is { statusCode: >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous } result)
            {
                HtmlDocument document= result.CreateDocument();
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract HAP sucks with nullable types
                if (document.DocumentNode.SelectSingleNode("//button[contains(text(),\"No Data\")]") is not null) // No results found
                    break;
                
                ret.AddRange(document.GetNodesWith("q4_9")?.Select(n => ParseSingleMangaFromSearchResultsList(baseUri, n))??[]);
            }else
                return null;
        }

        return ret.ToArray();
    }

    private (Manga, MangaConnectorId<Manga>) ParseSingleMangaFromSearchResultsList(Uri baseUri, HtmlNode resultNode)
    {
        HtmlNode titleAndLinkNode = resultNode.SelectSingleNode("//a[contains(@href,'title')]");
        string link = titleAndLinkNode.Attributes["href"].Value;

        return ((Manga, MangaConnectorId<Manga>))GetMangaFromUrl(new Uri(baseUri, link).ToString())!;
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
        foreach (string uri in BaseUris)
            if (GetMangaFromIdWithDomain(mangaIdOnSite, uri) is { } result)
                return result;
        return null;
    }

    private (Manga, MangaConnectorId<Manga>)? GetMangaFromIdWithDomain(string mangaIdOnSite, string domain)
    {
        Uri baseUri = new ($"https://{domain}/");
        return GetMangaFromUrl(new Uri(baseUri, $"title/{mangaIdOnSite}").ToString());
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
        if (downloadClient.MakeRequest(url, RequestType.Default) is
            { statusCode: >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous } result)
        {
            HtmlDocument document= result.CreateDocument();

            if (document.GetNodeWith("q1_1")?.GetAttributeValue("title", string.Empty) is not { Length: >0 } name)
            {
                Log.Debug("Name not found.");
                return null;
            }
            string description = document.GetNodeWith("0a_9")?.InnerText ?? string.Empty;

            if (document.GetNodeWith("q1_1")?.GetAttributeValue("src", string.Empty) is not { Length: >0 } coverRelative)
            {
                Log.Debug("Cover not found.");
                return null;
            }
            string coverUrl = $"{url[..url.IndexOf('/', 9)]}{coverRelative}";

            MangaReleaseStatus releaseStatus = document.GetNodeWith("Yn_5")?.InnerText.ToLower() switch
            {
                "pending" => MangaReleaseStatus.Unreleased,
                "ongoing" => MangaReleaseStatus.Continuing,
                "completed" => MangaReleaseStatus.Completed,
                "hiatus" => MangaReleaseStatus.OnHiatus,
                "cancelled" => MangaReleaseStatus.Cancelled,
                _ => MangaReleaseStatus.Unreleased
            };
            
            ICollection<Author> authors = document.GetNodeWith("tz_4")?
                .ChildNodes.Where(n => n.Name == "a")
                .Select(n => n.InnerText)
                .Select(t => new Author(t))
                .ToList()??[];

            ICollection<MangaTag> mangaTags = document.GetNodesWith("kd_0")?
                .Select(n => n.InnerText)
                .Select(t => new MangaTag(t))
                .ToList()??[];

            ICollection<Link> links = [];

            ICollection<AltTitle> altTitles = document.GetNodeWith("tz_2")?
                .ChildNodes.Where(n => n.InnerText.Trim().Length > 1)
                .Select(n => n.InnerText)
                .Select(t => new AltTitle(string.Empty, t))
                .ToList()??[];
            
            Manga m = new (name, description, coverUrl, releaseStatus, authors, mangaTags, links, altTitles);
            MangaConnectorId<Manga> mcId = new(m, this, url.Split('/').Last(), url);
            m.MangaConnectorIds.Add(mcId);
            return (m, mcId);
        }
        else return null;
    }

    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> mangaId, string? language = null)
    {
        foreach (string uri in BaseUris)
            if (GetChaptersFromDomain(mangaId, uri) is { } result)
                return result;
        return [];
    }

    private (Chapter, MangaConnectorId<Chapter>)[]? GetChaptersFromDomain(MangaConnectorId<Manga> mangaId, string domain)
    {
        Uri baseUri = new ($"https://{domain}/");
        Uri requestUri = new (baseUri, $"title/{mangaId.IdOnConnectorSite}");

        List<(Chapter, MangaConnectorId<Chapter>)> ret = [];
        
        if (downloadClient.MakeRequest(requestUri.ToString(), RequestType.Default) is
            { statusCode: >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous } result)
        {
            HtmlDocument document= result.CreateDocument();

            if (document.GetNodesWith("8t_8") is not { } chapterNodes)
            {
                Log.Debug("No chapters found.");
                return null;
            }
            
            foreach (HtmlNode chapterNode in chapterNodes)
            {
                if(ParseChapter(mangaId.Obj, chapterNode, baseUri) is { } ch)
                    ret.Add(ch);
            }
        }
        else return null;

        return ret.ToArray();
    }

    private static readonly Regex VolChTitleRex = new(@"(?:.*(?:Vol\.?(?:ume)?)\s*([0-9]+))?.*(?:Ch\.?(?:apter)?)\s*((?:\d+\.)*[0-9]+)\s*(?::|-\s+(.*))?", RegexOptions.Compiled);
    private (Chapter, MangaConnectorId<Chapter>)? ParseChapter(Manga manga, HtmlNode chapterNode, Uri baseUri)
    {
        HtmlNode linkNode = chapterNode.SelectSingleNode("./div[1]/a");
        Match linkMatch = VolChTitleRex.Match(linkNode.InnerText);
        HtmlNode? titleNode = chapterNode.SelectSingleNode("./div[1]/span");
        
        string chapterNumber;
        int? volumeNumber = null;

        if (!linkMatch.Success || !linkMatch.Groups[2].Success)
        {
            Log.Debug($"Not in standard Volume/Chapter format: {linkNode.InnerText}");
            if (Match(linkNode.InnerText, @"[^\d]*((?:\d+\.)*\d+)[^\d]*") is not { Success: true } match)
            {
                Log.Debug($"Unable to parse chapter-number: {linkNode.InnerText}");
                return null;
            }
            chapterNumber = match.Groups[1].Value;
        }
        else
        {
            chapterNumber = linkMatch.Groups[2].Value;
            volumeNumber = linkMatch.Groups[1].Success ? int.Parse(linkMatch.Groups[1].Value) : null;
        }
        
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract HAP sucks with nullables
        string? title = titleNode is not null ? titleNode.InnerText[2..] : (linkMatch.Groups[3].Success ? linkMatch.Groups[3].Value : null);

        string url = new Uri(baseUri, linkNode.GetAttributeValue("href", "")).ToString();
        string id = linkNode.GetAttributeValue("href", "")[7..];
            
        Chapter chapter = new (manga, chapterNumber, volumeNumber, title);
        MangaConnectorId<Chapter> chId = new(chapter, this, id, url);
        chapter.MangaConnectorIds.Add(chId);
        
        return (chapter, chId);
    }

    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        foreach (string uri in BaseUris)
            if (GetChapterImageUrlsFromDomain(chapterId, uri) is { } result)
                return result;
        return [];
    }

    private string[]? GetChapterImageUrlsFromDomain(MangaConnectorId<Chapter> chapterId, string domain)
    {
        Uri baseUri = new ($"https://{domain}/");
        Uri requestUri = new (baseUri, $"title/{chapterId.IdOnConnectorSite}");
        if (downloadClient.MakeRequest(requestUri.ToString(), RequestType.Default) is
            { statusCode: >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous } result)
        {
            HtmlDocument document = result.CreateDocument();

            if (document.DocumentNode.SelectSingleNode("//script[@type='qwik/json']")?.InnerText is not { } imageJson)
            {
                Log.Debug("No images found.");
                return null;
            }

            MatchCollection matchCollection = Matches(imageJson, @"https?:\/\/[\da-zA-Z\.]+\/[^,""]*\.[a-z]+");
            return matchCollection.Select(m => m.Value).ToArray();
        }
        else return null;
    }
}

internal static class MangaParkHelper
{
    internal static HtmlDocument CreateDocument(this RequestResult result)
    {
        HtmlDocument document = new();
        StreamReader sr = new (result.result);
        string htmlStr = sr.ReadToEnd().Replace("q:key", "qkey");
        document.LoadHtml(htmlStr);
        
        return document;
    }

    internal static HtmlNode? GetNodeWith(this HtmlDocument document, string search) => document.DocumentNode.SelectSingleNode("/html").GetNodeWith(search);
    internal static HtmlNode? GetNodeWith(this HtmlNode node, string search) => node.SelectNodes($"{node.XPath}//*[@qkey='{search}']")?.FirstOrDefault();
    internal static HtmlNodeCollection? GetNodesWith(this HtmlDocument document, string search) => document.DocumentNode.SelectSingleNode("/html ").GetNodesWith(search);
    // ReSharper disable once ReturnTypeCanBeNotNullable HAP nullable
    internal static HtmlNodeCollection? GetNodesWith(this HtmlNode node, string search) => node.SelectNodes($"{node.XPath}//*[@qkey='{search}']");
}