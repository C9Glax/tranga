using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using HtmlAgilityPack;
using static System.Text.RegularExpressions.Regex;

namespace API.MangaConnectors;

public class Bato : MangaConnector
{
    public Bato() : base(nameof(Bato),
        ["en"],
        ["bato.to"],
        "https://bato.to/public-assets/img/bato-favicon.ico?v1=")
    {
        this.downloadClient = new HttpDownloadClient();
    }

    #region Search -------------------------------------------------------------

    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        foreach (var uri in BaseUris)
            if (SearchMangaWithDomain(mangaSearchName, uri) is { } result)
                return result;
        return [];
    }

    private (Manga, MangaConnectorId<Manga>)[]? SearchMangaWithDomain(string name, string domain)
    {
        Log.DebugFormat("Using Bato search on domain {0}", domain);
        var baseUri = new Uri($"https://{domain}/");

        var ret = new List<(Manga, MangaConnectorId<Manga>)>();
        for (int page = 1;; page++)
        {
            var searchUri = new Uri(baseUri,
                $"v3x-search?word={HttpUtility.UrlEncode(name)}&page={page}");

            if (downloadClient.MakeRequest(searchUri.ToString(), RequestType.Default).Result
                    is not { StatusCode: >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous } response)
                return null;
            var doc = response.CreateDocument();

            if (doc.DocumentNode.SelectSingleNode("//button[contains(text(),\"No Data\")]") is not null)
                break;

            var resultNodes = doc.GetNodesWith("q4_9");
            if (resultNodes is not { Count: > 0 })
                break;

            var urls = resultNodes
                .Select(node => node.SelectSingleNode("//a[contains(@href,'title')]")?.Attributes["href"]?.Value)
                .Where(u => u != null)!
                .Distinct();

            foreach (var rel in urls)
            {
                var abs = new Uri(baseUri, rel).ToString();
                var mangaInfo = GetMangaFromUrl(abs);
                if (mangaInfo != null) ret.Add(mangaInfo.Value);
            }
        }

        return ret.DistinctBy(r => r.Item1.Key).ToArray();
    }

    #endregion

    #region Get manga by id ----------------------------------------------------

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
        foreach (var uri in BaseUris)
            if (GetMangaFromIdWithDomain(mangaIdOnSite, uri) is { } result)
                return result;
        return null;
    }

    private (Manga, MangaConnectorId<Manga>)? GetMangaFromIdWithDomain(string mangaIdOnSite, string domain)
    {
        var baseUri = new Uri($"https://{domain}/");
        return GetMangaFromUrl(new Uri(baseUri, $"title/{mangaIdOnSite}").ToString());
    }

    #endregion

    #region Parse manga page ----------------------------------------------------

    private readonly Regex _urlRex = new(@"^(https?:\/\/[^\/]+)\/title\/(\d+)(?:-[^\/]*)?(?:\/([^\/]+))?.*$");

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
        if (_urlRex.Match(url) is not { Success: true } matched || !matched.Groups[2].Success)
            return null;

        var baseUrl   = matched.Groups[1].Value;
        var mangaId   = matched.Groups[2].Value; 

        if (downloadClient.MakeRequest($"{baseUrl}/title/{mangaId}", RequestType.Default).Result
                is { StatusCode: >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous } response)
        {
            var doc = response.CreateDocument();

            var nameNode = doc.GetNodeWith("q1_1");
            if (nameNode?.GetAttributeValue("title", string.Empty) is not { Length: > 0 } rawName)
                return null;
            var name = HttpUtility.HtmlDecode(rawName);

            var desc = HttpUtility.HtmlDecode(doc.GetNodeWith("0a_9")?.InnerText ?? string.Empty);

            if (nameNode.GetAttributeValue("src", string.Empty) is not { Length: > 0 } coverRel)
                return null;
            var coverUrl = $"{baseUrl}{coverRel}";

            var statusStr = doc.GetNodeWith("Yn_5")?.InnerText.ToLowerInvariant();
            MangaReleaseStatus releaseStatus = statusStr switch
            {
                "pending"   => MangaReleaseStatus.Unreleased,
                "ongoing"   => MangaReleaseStatus.Continuing,
                "completed" => MangaReleaseStatus.Completed,
                "hiatus"    => MangaReleaseStatus.OnHiatus,
                "cancelled" => MangaReleaseStatus.Cancelled,
                _           => MangaReleaseStatus.Unreleased
            };

            var authors = doc.GetNodeWith("tz_4")?
                .ChildNodes.Where(n => n.Name == "a")
                .Select(n => new Author(HttpUtility.HtmlDecode(n.InnerText)))
                .ToList() ?? new List<Author>();

            var mangaTags = doc.GetNodesWith("kd_0")?
                .SelectMany(n =>
                {
                    var txt = HttpUtility.HtmlDecode(n.InnerText);
                    return txt.Split('•').Select(t => t.Trim());
                })
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => new MangaTag(t))
                .ToList() ?? new List<MangaTag>();

            var altTitles = doc.GetNodeWith("tz_2")?
                .ChildNodes.Where(n => n.InnerText.Trim().Length > 1)
                ?.SelectMany(n =>
                {
                    var txt = HttpUtility.HtmlDecode(n.InnerText);
                    return txt.Split('•').Select(t => t.Trim());
                })
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => new AltTitle(string.Empty, t))
                .ToList() ?? new List<AltTitle>();

            var links = new List<Link>();

            var manga = new Manga(name, desc, coverUrl, releaseStatus, authors, mangaTags, links, altTitles);

            var mcId = new MangaConnectorId<Manga>(manga, this, mangaId, $"{baseUrl}/title/{mangaId}");
            manga.MangaConnectorIds.Add(mcId);
            return (manga, mcId);
        }
        else
            return null;
    }

    #endregion

    #region Chapters -----------------------------------------------------------

    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> mangaId, string? language = null)
    {
        foreach (var uri in BaseUris)
            if (GetChaptersFromDomain(mangaId, uri) is { } result)
                return result;
        return [];
    }

    private (Chapter, MangaConnectorId<Chapter>)[]? GetChaptersFromDomain(MangaConnectorId<Manga> mangaId, string domain)
    {
        Log.DebugFormat("Getting chapters from Bato domain {0}", domain);
        var baseUri = new Uri($"https://{domain}/");
        var requestUri = new Uri(baseUri, $"title/{mangaId.IdOnConnectorSite}");

        if (downloadClient.MakeRequest(requestUri.ToString(), RequestType.Default).Result
                is { StatusCode: >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous } response)
        {
            var doc = response.CreateDocument();

            var chapterNodes = doc.GetNodesWith("8t_8");
            if (chapterNodes is null) return null;

            var ret = new List<(Chapter, MangaConnectorId<Chapter>)>();
            foreach (var node in chapterNodes)
                if (ParseChapter(mangaId.Obj, node, baseUri) is { } ch)
                    ret.Add(ch);
            return ret.ToArray();
        }
        else
            return null;
    }

    private static readonly Regex VolChTitleRex =
        new(@"(?:.*(?:Vol\.?(?:ume)?)\s*([0-9]+))?.*(?:Ch\.?(?:apter)?)\s*((?:\d+\.)*[0-9]+)\s*(?::|-\s+(.*))?");

    private (Chapter, MangaConnectorId<Chapter>)? ParseChapter(Manga manga, HtmlNode chapterNode, Uri baseUri)
    {
        var linkNode = chapterNode.SelectSingleNode("./div[1]/a");
        if (linkNode == null) return null;

        var linkText = HttpUtility.HtmlDecode(linkNode.InnerText);
        var match    = VolChTitleRex.Match(linkText);

        string chapterNumber;
        int? volumeNumber = null;

        if (!match.Success || !match.Groups[2].Success)
        {
            var simpleMatch = Match(linkText, @"[^\d]*((?:\d+\.)*\d+)[^\d]*");
            if (!simpleMatch.Success) return null;
            chapterNumber = simpleMatch.Groups[1].Value;
        }
        else
        {
            chapterNumber = match.Groups[2].Value;
            if (match.Groups[1].Success)
                volumeNumber = int.Parse(match.Groups[1].Value);
        }

        var titleNode = chapterNode.SelectSingleNode("./div[1]/span");
        string? title = null;
        if (titleNode != null)
            title = HttpUtility.HtmlDecode(titleNode.InnerText)[2..];
        else if (match.Groups[3].Success)
            title = match.Groups[3].Value;

        var relUrl = linkNode.GetAttributeValue("href", string.Empty);
        var absUrl = new Uri(baseUri, relUrl).ToString();

        if (_urlRex.Match(absUrl) is not { Success: true } urlMatch ||
            !urlMatch.Groups[2].Success || !urlMatch.Groups[3].Success)
            return null;

        var mangaId   = urlMatch.Groups[2].Value; 
        var chapterId = urlMatch.Groups[3].Value; 

        string connectorId = $"{mangaId}/{chapterId}";

        var chapter = new Chapter(manga, chapterNumber, volumeNumber, title);
        var chId    = new MangaConnectorId<Chapter>(chapter, this, connectorId, absUrl); 
        chapter.MangaConnectorIds.Add(chId);
        return (chapter, chId);
    }

    #endregion

    #region Image URLs ---------------------------------------------------------

    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        foreach (var uri in BaseUris)
            if (GetChapterImageUrlsFromDomain(chapterId, uri) is { } result)
                return result;
        return [];
    }

    private string[]? GetChapterImageUrlsFromDomain(MangaConnectorId<Chapter> chapterId, string domain)
    {
        Log.DebugFormat("Fetching images for chapter {0} on domain {1}", chapterId.IdOnConnectorSite, domain);
        var baseUri = new Uri($"https://{domain}/");
        var requestUri = new Uri(baseUri, $"title/{chapterId.IdOnConnectorSite}");

        if (downloadClient.MakeRequest(requestUri.ToString(), RequestType.Default).Result
                is { StatusCode: >= HttpStatusCode.OK and < HttpStatusCode.Ambiguous } response)
        {
            var doc = response.CreateDocument();

            var jsonNode = doc.DocumentNode.SelectSingleNode("//script[@type='qwik/json']");
            if (jsonNode?.InnerText is not { Length: > 0 } json) return null;

            var matches = Matches(json, @"""https?:\/\/[\da-zA-Z\.]+\/[^,]*\.[a-z]+""");
            return matches.Select(m => m.Value.Trim('"')).ToArray();
        }
        else
            return null;
    }

    #endregion
}

internal static class BatoHelper
{
    internal static HtmlDocument CreateDocument(this HttpResponseMessage result)
    {
        var doc = new HtmlDocument();
        using var sr = new StreamReader(result.Content.ReadAsStream());
        var html = sr.ReadToEnd().Replace("q:key", "qkey");
        doc.LoadHtml(html);
        return doc;
    }

    internal static HtmlNode? GetNodeWith(this HtmlDocument document, string search) =>
        document.DocumentNode.SelectSingleNode("/html").GetNodeWith(search);

    internal static HtmlNode? GetNodeWith(this HtmlNode node, string search) =>
        node.SelectNodes($"{node.XPath}//*[@qkey='{search}']")?.FirstOrDefault();

    internal static HtmlNodeCollection? GetNodesWith(this HtmlDocument document, string search) =>
        document.DocumentNode.SelectSingleNode("/html ").GetNodesWith(search);

    internal static HtmlNodeCollection? GetNodesWith(this HtmlNode node, string search) =>
        node.SelectNodes($"{node.XPath}//*[@qkey='{search}']");
}
