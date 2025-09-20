using System.Net;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using HtmlAgilityPack;

namespace API.MangaConnectors;

public class MangaPark : MangaConnector
{
    public MangaPark() : base("MangaPark", 
        ["en"],
        ["mangapark.com", "mangapark.net", "mangapark.org", "mangapark.me", "mangapark.io", "mangapark.to", "comicpark.org", "comicpark.to", "readpark.org", "readpark.net", "parkmanga.com", "parkmanga.net", "parkmanga.org", "mpark.to"], 
        "https://mangapark.com/static-assets/img/favicon.ico")
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
        Uri baseUri = new ($"https://{domain}/");
        Uri search = new(baseUri, $"search?word={mangaSearchName}&lang={Tranga.Settings.DownloadLanguage}");
        
        HtmlDocument document = new();
        List<(Manga, MangaConnectorId<Manga>)> ret = [];
        
        for (int page = 1;; page++) // break; in loop
        {
            Uri pageSearch = new(search, $"&page={page}");
            if (downloadClient.MakeRequest(pageSearch.ToString(), RequestType.Default) is { statusCode: >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous } result)
            {
                document.Load(result.result);
                // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract HAP sucks with nullable types
                if (document.DocumentNode.SelectSingleNode("//button[contains(text(),\"No Data\")]") is not null) // No results found
                    break;

                HtmlNode resultsListNode = document.GetNodeWith("jp_1");
                ret.AddRange(resultsListNode.ChildNodes.Select(n => ParseSingleMangaFromSearchResultsList(baseUri, n)));
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
            HtmlDocument document = new();
            document.Load(result.result);

            string name = document.GetNodeWith("2x", "q:id").InnerText;
            string description = document.GetNodeWith("0a_9").InnerText;

            string coverRelative = document.GetNodeWith("q1_1").GetAttributeValue("src", "");
            string coverUrl = $"{url.Substring(0, url.IndexOf('/', 9))}{coverRelative}";

            MangaReleaseStatus releaseStatus = document.GetNodeWith("Yn_5").InnerText.ToLower() switch
            {
                "pending" => MangaReleaseStatus.Unreleased,
                "ongoing" => MangaReleaseStatus.Continuing,
                "completed" => MangaReleaseStatus.Completed,
                "hiatus" => MangaReleaseStatus.OnHiatus,
                "cancelled" => MangaReleaseStatus.Cancelled,
                _ => MangaReleaseStatus.Unreleased
            };
            
            ICollection<Author> authors = document.GetNodeWith("tz_4")
                .ChildNodes.Where(n => n.Name == "a")
                .Select(n => n.InnerText)
                .Select(t => new Author(t)).ToList();

            ICollection<MangaTag> mangaTags = document.GetNodesWith("kd_0")
                .Select(n => n.InnerText)
                .Select(t => new MangaTag(t)).ToList();

            ICollection<Link> links = [];

            ICollection<AltTitle> altTitles = document.GetNodeWith("tz_2")
                .ChildNodes.Where(n => n.InnerText.Length > 1)
                .Select(n => n.InnerText)
                .Select(t => new AltTitle(string.Empty, t)).ToList();
            
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
        if (downloadClient.MakeRequest(requestUri.ToString(), RequestType.Default) is
            { statusCode: >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous } result)
        {
            HtmlDocument document = new();
            document.Load(result.result);
            
            HtmlNodeCollection chapterNodes = document.GetNodesWith("8t_8");

            return chapterNodes.Select(n => ParseChapter(mangaId.Obj, n, baseUri)).ToArray();
        }
        else return null;
    }

    private readonly Regex _volChTitleRex = new(@"(?:.*(?:Vol\.?(?:ume)?)\s*([0-9]+))?.*(?:Ch\.?(?:apter)?)\s*([0-9\.]+)(?::\s+(.*))?");
    private (Chapter, MangaConnectorId<Chapter>) ParseChapter(Manga manga, HtmlNode chapterNode, Uri baseUri)
    {
        HtmlNode linkNode = chapterNode.SelectSingleNode("/div[1]/a");
        Match linkMatch = _volChTitleRex.Match(linkNode.InnerText);
        HtmlNode? titleNode = chapterNode.SelectSingleNode("/div[1]/span");

        if (!linkMatch.Success || !linkMatch.Groups[2].Success)
        {
            Log.Error($"Unable to parse Chapter: {chapterNode.InnerHtml}");
            throw new ($"Unable to parse Chapter: {chapterNode.InnerHtml}");
        }
        
        string chapterNumber = linkMatch.Groups[2].Value;
        int? volumeNumber = linkMatch.Groups[1].Success ? int.Parse(linkMatch.Groups[1].Value) : null;
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
            HtmlDocument document = new();
            document.Load(result.result);
            
            HtmlNodeCollection imageNodes = document.GetNodesWith("8X_2");

            return imageNodes.Select(n => n.SelectSingleNode("/div/img").GetAttributeValue("src", "")).ToArray();
        }
        else return null;
    }
}

internal static class Helper
{
    internal static HtmlNode GetNodeWith(this HtmlDocument document, string search, string selector = "q:key") => document.DocumentNode.SelectSingleNode($"//*[@${selector}=${search}]");
    internal static HtmlNodeCollection GetNodesWith(this HtmlDocument document, string search, string selector = "q:key") => document.DocumentNode.SelectNodes($"//*[@${selector}=${search}]");
}