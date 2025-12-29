using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;
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
            "mangaworld.ac","www.mangaworld.ac",
            "mangaworld.mx","www.mangaworld.mx"
        ],
        "https://www.mangaworld.mx/public/assets/seo/favicon-96x96.png?v=3"
    )
    {
        downloadClient = new HttpDownloadClient();
    }

    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        // 1) Tentativo con la stringa così com'è
        (Manga, MangaConnectorId<Manga>)[] first = SearchOnce(mangaSearchName);
        if (first.Length > 0)
            return first;

        // 2) Fallback: rimuovi diacritici / caratteri strani
        string fallback = RemoveDiacritics(mangaSearchName);
        if (!string.Equals(fallback, mangaSearchName, StringComparison.Ordinal))
            return SearchOnce(fallback);

        return first;
    }

    private (Manga, MangaConnectorId<Manga>)[] SearchOnce(string query)
    {
        Uri baseUri = new("https://www.mangaworld.mx/");
        Uri searchUrl = new(baseUri, "archive?keyword=" + HttpUtility.UrlEncode(query));

        using HttpResponseMessage res =
            downloadClient.MakeRequest(searchUrl.ToString(), RequestType.Default).Result;

        if ((int)res.StatusCode < 200 || (int)res.StatusCode >= 300)
            return [];

        using StreamReader sr = new(res.Content.ReadAsStream());
        string html = sr.ReadToEnd();

        HtmlDocument doc = new();
        doc.LoadHtml(html);

        HtmlNodeCollection? anchors = doc.DocumentNode.SelectNodes("//a[@href and contains(@href,'/manga/')]");
        if (anchors is null || anchors.Count < 1)
            return [];

        List<(Manga, MangaConnectorId<Manga>)> list = new();
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        foreach (HtmlNode a in anchors)
        {
            string href = a.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(href))
                continue;

            string canonical = new Uri(baseUri, href).ToString();

            if (!seen.Add(canonical))
                continue;

            (Manga, MangaConnectorId<Manga>)? manga = GetMangaFromUrl(canonical);
            if (manga is null)
                continue;

            list.Add(manga.Value);
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

        Uri seriesUrl = new Uri($"https://www.mangaworld.mx/manga/{id}/{slug}/");

        using HttpResponseMessage res =
            downloadClient.MakeRequest(seriesUrl.ToString(), RequestType.MangaInfo).Result;

        if ((int)res.StatusCode < 200 || (int)res.StatusCode >= 300)
            return null;

        using StreamReader sr = new StreamReader(res.Content.ReadAsStream());
        string html = sr.ReadToEnd();

        HtmlDocument doc = new HtmlDocument();
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
            cover = MakeAbsoluteUrl(seriesUrl, cover);

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

        // Generi (badge/link dopo l'etichetta "Generi:")
        List<MangaTag> tags =
            doc.DocumentNode
               .SelectNodes("//span[normalize-space(text())='Generi:']/following-sibling::a")
               ?.Select(a => HtmlEntity.DeEntitize(a.InnerText).Trim())
               .Where(s => !string.IsNullOrWhiteSpace(s))
               .Distinct(StringComparer.OrdinalIgnoreCase)
               .Select(s => new MangaTag(s))
               .ToList()
            ?? [];

        Manga m = new Manga(
            HtmlEntity.DeEntitize(title).Trim(),
            description,
            cover,
            status,
            [],
            tags,
            [],
            [],
            originalLanguage: "it");

        MangaConnectorId<Manga> mcId = new MangaConnectorId<Manga>(m, this, $"{id}/{slug}", seriesUrl.ToString());
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
        string seriesUrl = $"https://www.mangaworld.mx/manga/{id}/{slug}/";

        string html = FetchHtmlWithFallback(seriesUrl, out Uri baseUri);
        if (string.IsNullOrEmpty(html))
            return [];

        html = Regex.Replace(html, @"<!--M#.*?-->", "", RegexOptions.Singleline);

        HtmlDocument doc = new HtmlDocument();
        doc.LoadHtml(html);

        List<(Chapter, MangaConnectorId<Chapter>)> chapters = ParseChaptersFromHtml(mangaId.Obj, doc, baseUri);
        return chapters.OrderBy(c => c.Item1, new Chapter.ChapterComparer()).ToArray();
    }

    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        string raw = chapterId.WebsiteUrl ?? $"https://www.mangaworld.mx/manga/{chapterId.IdOnConnectorSite}";
        string url = EnsureReaderUrl(raw);

        if (downloadClient.MakeRequest(url, RequestType.MangaInfo).Result is not { IsSuccessStatusCode: true } res)
            return [];

        string html = res.Content.ReadAsStringAsync().Result;

        Uri baseUri = new(url);
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        HtmlNodeCollection imageNodes = doc.DocumentNode.SelectNodes("//img[@data-src or @src or @srcset]") ?? new HtmlNodeCollection(null);
        IEnumerable<string> fromDom = imageNodes
            .SelectMany(i =>
            {
                List<string> list = [];
                string ds = i.GetAttributeValue("data-src", "");
                string s = i.GetAttributeValue("src", "");
                string ss = i.GetAttributeValue("srcset", "");

                if (!string.IsNullOrEmpty(ds))
                    list.Add(ds);
                if (!string.IsNullOrEmpty(s))
                    list.Add(s);
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
            .Where(u => u.ToLowerInvariant().StartsWith("http") && (u.EndsWith(".jpg") || u.EndsWith(".jpeg") || u.EndsWith(".png") || u.EndsWith(".webp")));

        return fromDom.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static readonly Regex SeriesUrl = new Regex(@"https?://[^/]+/manga/(?<id>\d+)/(?<slug>[^/]+)/?", RegexOptions.IgnoreCase);

    private List<(Chapter, MangaConnectorId<Chapter>)> ParseChaptersFromHtml(Manga manga, HtmlDocument document, Uri baseUri)
    {
        List<(Chapter, MangaConnectorId<Chapter>)> ret = new();
        HashSet<string> seen = new(StringComparer.OrdinalIgnoreCase);

        HtmlNodeCollection? volumeElements = document.DocumentNode.SelectNodes("//div[contains(@class,'volume-element')]");
        if (volumeElements is { Count: > 0 })
        {
            foreach (HtmlNode volNode in volumeElements)
            {
                int volumeNumber = 0;
                string volText = volNode.SelectSingleNode(".//p[contains(@class,'volume-name')]")?.InnerText ?? "";
                Match vm = Regex.Match(volText, @"[Vv]olume\s+([0-9]+)");
                if (vm.Success && int.TryParse(vm.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out int volParsed))
                    volumeNumber = volParsed;

                HtmlNodeCollection chapterNodes = volNode.SelectNodes(".//div[contains(@class,'chapter')]/a[@href]") ?? new HtmlNodeCollection(null);
                foreach (HtmlNode ch in chapterNodes)
                    TryAddChapterNode(manga, ch, baseUri, volumeNumber, ret, seen);
            }
        }

        if (ret.Count != 0)
            return ret;
        if (document.DocumentNode.SelectNodes("//div[contains(@class,'chapters-wrapper')]//a[contains(@class,'chap')]") is not { Count: > 0 } flatNodes)
            return ret;

        foreach (HtmlNode a in flatNodes)
            TryAddChapterNode(manga, a, baseUri, 0, ret, seen);

        return ret;
    }

    private void TryAddChapterNode(Manga manga, HtmlNode anchor, Uri baseUri, int volumeNumber,
        List<(Chapter, MangaConnectorId<Chapter>)> acc, HashSet<string> dedup)
    {
        string label = anchor.SelectSingleNode(".//span")?.InnerText ?? anchor.InnerText ?? "";
        Match cm = Regex.Match(label, @"(?:[Cc]apitolo|[Cc]hapter)\s*([0-9]+(?:\.[0-9]+)?)");
        if (!cm.Success) return;

        // parse numero capitolo come decimal; format invariant al confine
        string chapterText = cm.Groups[1].Value.Trim();
        if (!decimal.TryParse(chapterText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal chNum))
            return;
        string chapterNumber = chNum.ToString(CultureInfo.InvariantCulture);

        string href = anchor.GetAttributeValue("href", "");
        if (string.IsNullOrWhiteSpace(href))
        {
            string raw = anchor.OuterHtml;
            Match mHref = Regex.Match(raw, @"href\s*=\s*[""']?([^'""\s>]+)", RegexOptions.IgnoreCase);
            if (mHref.Success)
                href = mHref.Groups[1].Value;
        }

        if (string.IsNullOrWhiteSpace(href)) return;

        string abs = MakeAbsoluteUrl(baseUri, href);
        string ensured = EnsureReaderUrl(abs);

        Match idMatch = Regex.Match(ensured, @"manga\/([0-9]+\/[a-z0-9\-]+\/read\/[a-z0-9]+)", RegexOptions.IgnoreCase);
        if (!idMatch.Success) return;

        string id = idMatch.Groups[1].Value;
        if (!dedup.Add(id)) return;

        Chapter chapter = new Chapter(manga, chapterNumber, volumeNumber);
        MangaConnectorId<Chapter> chId = new MangaConnectorId<Chapter>(chapter, this, id, ensured);
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

    private static string RemoveDiacritics(string s)
    {
        if (string.IsNullOrEmpty(s))
            return s;

        string norm = s.Normalize(NormalizationForm.FormD);
        Span<char> buffer = stackalloc char[norm.Length];
        int i = 0;
        foreach (char c in norm)
        {
            UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                buffer[i++] = c;
        }
        return new string(buffer[..i]).Normalize(NormalizationForm.FormC);
    }

    private string FetchHtmlWithFallback(string seriesUrl, out Uri baseUri)
    {
        baseUri = new Uri(seriesUrl);
        HttpResponseMessage res;
        try
        {
            res = downloadClient.MakeRequest(seriesUrl, RequestType.Default).Result;
        }
        catch
        {
            return "";
        }
        using StreamReader sr = new StreamReader(res.Content.ReadAsStream());
        return sr.ReadToEnd();
    }
}

