using System.Text.RegularExpressions;
using System.Web;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using HtmlAgilityPack;

namespace API.MangaConnectors;

public sealed class Mangaworld : MangaConnector
{
    public Mangaworld() : base(
        "Mangaworld",
        ["it"],
        [
            "mangaworld.cx","www.mangaworld.cx",
            "mangaworld.bz","www.mangaworld.bz",
            "mangaworld.fun","www.mangaworld.fun",
            "mangaworld.ac","www.mangaworld.ac"
        ],
        "https://www.mangaworld.cx/public/assets/seo/favicon-96x96.png?v=3"
    )
    {
        downloadClient = new HttpDownloadClient();
    }

    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        Uri baseUri = new("https://www.mangaworld.cx/");
        Uri searchUrl = new(baseUri, "archive?keyword=" + HttpUtility.UrlEncode(mangaSearchName));

        HttpResponseMessage res;
        try
        {
            res = downloadClient.MakeRequest(searchUrl.ToString(), RequestType.Default).Result;
        }
        catch
        {
            return [];
        }
        if ((int)res.StatusCode < 200 || (int)res.StatusCode >= 300)
            return [];

        using StreamReader sr = new(res.Content.ReadAsStream());
        string html = sr.ReadToEnd();

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        HtmlNodeCollection? anchors = doc.DocumentNode.SelectNodes("//a[@href and contains(@href,'/manga/')]");
        if (anchors is null || anchors.Count < 1)
            return [];

        List<(Manga, MangaConnectorId<Manga>)> list = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (HtmlNode a in anchors)
        {
            string href = a.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(href))
                continue;

            string canonical = new Uri(baseUri, href).ToString();

            // Evita duplicati
            if (!seen.Add(canonical))
                continue;

            (Manga, MangaConnectorId<Manga>)? manga = GetMangaFromUrl(canonical);
            if (manga is null)
                continue;

            list.Add(((Manga, MangaConnectorId<Manga>))manga);
        }

        return list.ToArray();
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
        Match m = SeriesUrl.Match(url);
        if (!m.Success)
            return null;
        return GetMangaFromId($"{m.Groups["id"].Value}/{m.Groups["slug"].Value}");
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
        string[] parts = mangaIdOnSite.Split('/', 2);
        if (parts.Length != 2)
            return null;

        string id = parts[0];
        string slug = parts[1];
        string url = $"https://www.mangaworld.cx/manga/{id}/{slug}/";

        HttpResponseMessage res;
        try
        {
            res = downloadClient.MakeRequest(url, RequestType.MangaInfo).Result;
        }
        catch
        {
            return null;
        }
        if ((int)res.StatusCode < 200 || (int)res.StatusCode >= 300)
            return null;

        using StreamReader sr = new(res.Content.ReadAsStream());
        string html = sr.ReadToEnd();

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        string title =
            doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null)
            ?? doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim()
            ?? slug.Replace('-', ' ');

        title = CleanTitleSuffix(title);

        string cover =
            doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", null)
            ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class,'cover')]//img")?.GetAttributeValue("src", null)
            ?? string.Empty;

        if (!string.IsNullOrEmpty(cover))
            cover = MakeAbsoluteUrl(new Uri(url), cover);

        string description =
            doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", null)
            ?? HtmlEntity.DeEntitize(
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'description') or contains(@class,'trama')]")
                ?.InnerText ?? string.Empty
            ).Trim();

        MangaReleaseStatus status = MangaReleaseStatus.Unreleased;
        string? detailRawStatus = ExtractItalianStatus(doc);
        if (!string.IsNullOrWhiteSpace(detailRawStatus))
            status = MapItalianStatus(detailRawStatus);

        Manga m = new(
            HtmlEntity.DeEntitize(title).Trim(),
            description,
            cover,
            status,
            [],
            [],
            [],
            [],
            originalLanguage: "it");

        MangaConnectorId<Manga> mcId = new(m, this, $"{id}/{slug}", url);
        m.MangaConnectorIds.Add(mcId);
        return (m, mcId);
    }

    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> mangaId, string? language = null)
    {
        string[] parts = mangaId.IdOnConnectorSite.Split('/', 2);
        if (parts.Length != 2)
            return [];

        string id = parts[0];
        string slug = parts[1];
        string seriesUrl = $"https://www.mangaworld.cx/manga/{id}/{slug}/";

        string html = FetchHtmlWithFallback(seriesUrl, out Uri baseUri);
        if (string.IsNullOrEmpty(html))
            return [];

        html = Regex.Replace(html, @"<!--M#.*?-->", "", RegexOptions.Singleline);

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        List<(Chapter, MangaConnectorId<Chapter>)> chapters = ParseChaptersFromHtml(mangaId.Obj, doc, baseUri);
        return chapters.OrderBy(c => c.Item1, new Chapter.ChapterComparer()).ToArray();
    }

    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        string raw = chapterId.WebsiteUrl ?? $"https://www.mangaworld.cx/manga/{chapterId.IdOnConnectorSite}";
        string url = EnsureReaderUrl(raw);

        HttpResponseMessage res;
        try
        {
            res = downloadClient.MakeRequest(url, RequestType.MangaInfo).Result;
        }
        catch
        {
            return [];
        }
        if ((int)res.StatusCode < 200 || (int)res.StatusCode >= 300)
            return [];

        using StreamReader sr = new(res.Content.ReadAsStream());
        string html = sr.ReadToEnd();

        Uri baseUri = new(url);
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        HtmlNodeCollection imageNodes = doc.DocumentNode.SelectNodes("//img[@data-src or @src or @srcset]") ?? new HtmlNodeCollection(null);
        IEnumerable<string> fromDom = imageNodes
            .SelectMany(i =>
            {
                var list = new List<string>();
                string ds = i.GetAttributeValue("data-src", "");
                string s = i.GetAttributeValue("src", "");
                string ss = i.GetAttributeValue("srcset", "");

                if (!string.IsNullOrEmpty(ds)) list.Add(ds);
                if (!string.IsNullOrEmpty(s)) list.Add(s);
                if (!string.IsNullOrEmpty(ss))
                {
                    foreach (string part in ss.Split(','))
                    {
                        string p = part.Trim().Split(' ')[0];
                        if (!string.IsNullOrWhiteSpace(p))
                            list.Add(p);
                    }
                }
                return list;
            })
            .Select(x => MakeAbsoluteUrl(baseUri, x))
            .Where(u => u.ToLowerInvariant().StartsWith("http") && (u.EndsWith(".jpg") || u.EndsWith(".png") || u.EndsWith(".webp")));

        return fromDom.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static readonly Regex SeriesUrl = new(@"https?://[^/]+/manga/(?<id>\d+)/(?<slug>[^/]+)/?", RegexOptions.IgnoreCase);

    private List<(Chapter, MangaConnectorId<Chapter>)> ParseChaptersFromHtml(Manga manga, HtmlDocument document, Uri baseUri)
    {
        List<(Chapter, MangaConnectorId<Chapter>)> ret = [];
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        var volumeElements = document.DocumentNode.SelectNodes("//div[contains(@class,'volume-element')]");
        if (volumeElements != null && volumeElements.Count > 0)
        {
            foreach (var volNode in volumeElements)
            {
                int volumeNumber = 0;
                var volText = volNode.SelectSingleNode(".//p[contains(@class,'volume-name')]")?.InnerText ?? "";
                var vm = Regex.Match(volText, @"[Vv]olume\s+([0-9]+)");
                if (vm.Success && int.TryParse(vm.Groups[1].Value, out int volParsed))
                    volumeNumber = volParsed;

                var chapterNodes = volNode.SelectNodes(".//div[contains(@class,'chapter')]/a[@href]") ?? new HtmlNodeCollection(null);
                foreach (var ch in chapterNodes)
                    TryAddChapterNode(manga, ch, baseUri, volumeNumber, ret, seen);
            }
        }

        if (ret.Count == 0)
        {
            var flatNodes = document.DocumentNode.SelectNodes("//div[contains(@class,'chapters-wrapper')]//a[contains(@class,'chap')]");
            if (flatNodes != null && flatNodes.Count > 0)
            {
                foreach (var a in flatNodes)
                    TryAddChapterNode(manga, a, baseUri, 0, ret, seen);
            }
        }

        return ret;
    }

    private void TryAddChapterNode(Manga manga, HtmlNode anchor, Uri baseUri, int volumeNumber,
        List<(Chapter, MangaConnectorId<Chapter>)> acc, HashSet<string> dedup)
    {
        string label = anchor.SelectSingleNode(".//span")?.InnerText ?? anchor.InnerText ?? "";
        var cm = Regex.Match(label, @"(?:[Cc]apitolo|[Cc]hapter)\s*([0-9]+(?:\.[0-9]+)?)");
        if (!cm.Success) return;

        string chapterNumber = cm.Groups[1].Value.Trim();

        string href = anchor.GetAttributeValue("href", "");
        if (string.IsNullOrWhiteSpace(href))
        {
            var raw = anchor.OuterHtml;
            var mHref = Regex.Match(raw, @"href\s*=\s*[""']?([^'""\s>]+)", RegexOptions.IgnoreCase);
            if (mHref.Success)
                href = mHref.Groups[1].Value;
        }

        if (string.IsNullOrWhiteSpace(href)) return;

        string abs = MakeAbsoluteUrl(baseUri, href);
        string ensured = EnsureReaderUrl(abs);

        var idMatch = Regex.Match(ensured, @"manga\/([0-9]+\/[a-z0-9\-]+\/read\/[a-z0-9]+)", RegexOptions.IgnoreCase);
        if (!idMatch.Success) return;

        string id = idMatch.Groups[1].Value;
        if (!dedup.Add(id)) return;

        Chapter chapter = new(manga, chapterNumber, volumeNumber);
        MangaConnectorId<Chapter> chId = new(chapter, this, id, ensured);
        chapter.MangaConnectorIds.Add(chId);
        acc.Add((chapter, chId));
    }

    private static string EnsureReaderUrl(string url)
    {
        int q = url.IndexOf('?');
        string basePart = q >= 0 ? url[..q] : url;
        string query = q >= 0 ? url[q..] : "";

        basePart = EnsureReaderUrlHasPage(basePart);

        if (!Regex.IsMatch(query, @"[?&]style=list\b", RegexOptions.IgnoreCase))
            query = string.IsNullOrEmpty(query) ? "?style=list" : (query + "&style=list");

        return basePart + query;
    }

    private static string MakeAbsoluteUrl(Uri baseUri, string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        s = s.Trim();
        if (s.StartsWith("//")) return "https:" + s;
        if (s.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return s;
        return new Uri(baseUri, s).ToString();
    }

    private static string EnsureReaderUrlHasPage(string url)
    {
        Match m = Regex.Match(url, @"(/read/[0-9a-fA-F]{16,64})(/(\d+))?");
        if (m.Success && string.IsNullOrEmpty(m.Groups[2].Value))
            url = url.TrimEnd('/') + "/1";
        return url;
    }

    private static string CleanTitleSuffix(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return t ?? "";
        return Regex.Replace(t, @"\s*(Scan\s\w+\s-\sMangaWorld)$", "", RegexOptions.IgnoreCase).Trim();
    }

    private static MangaReleaseStatus MapItalianStatus(string s) => s.Trim().ToLowerInvariant() switch
    {
        "in corso" or "ongoing" => MangaReleaseStatus.Continuing,
        "completo" or "concluso" or "finito" => MangaReleaseStatus.Completed,
        "in pausa" or "hiatus" => MangaReleaseStatus.OnHiatus,
        "droppato" or "cancellato" or "interrotto" => MangaReleaseStatus.Cancelled,
        _ => MangaReleaseStatus.Unreleased
    };

    private static string? ExtractItalianStatus(HtmlDocument doc)
    {
        HtmlNode? node = doc.DocumentNode.SelectSingleNode("//span[normalize-space(text())='Stato:']/following-sibling::*[1]");
        return node?.InnerText?.Trim();
    }

    private string FetchHtmlWithFallback(string seriesUrl, out Uri baseUri)
    {
        baseUri = new(seriesUrl);
        HttpResponseMessage res;
        try
        {
            res = downloadClient.MakeRequest(seriesUrl, RequestType.Default).Result;
        }
        catch
        {
            return "";
        }
        using StreamReader sr = new(res.Content.ReadAsStream());
        return sr.ReadToEnd();
    }
}
