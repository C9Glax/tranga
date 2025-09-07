using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public sealed class Mangaworld : MangaConnector
{
    public Mangaworld() : base(
        "Mangaworld",
        new[] { "it" },
        new[]
        {
            "mangaworld.cx","www.mangaworld.cx",
            "mangaworld.bz","www.mangaworld.bz",
            "mangaworld.fun","www.mangaworld.fun",
            "mangaworld.ac","www.mangaworld.ac"
        },
        "https://www.mangaworld.cx/favicon.ico"
    )
    {
        downloadClient = new HttpDownloadClient();
    }

    // ============================ SEARCH ============================
    public override Manga[] SearchManga(string mangaSearchName)
    {
        var baseUri = new Uri("https://www.mangaworld.cx/");
        var searchUrl = new Uri(baseUri, "archive?keyword=" + Uri.EscapeDataString(mangaSearchName));

        var res = downloadClient.MakeRequest(searchUrl.ToString(), RequestType.Default);
        if ((int)res.statusCode < 200 || (int)res.statusCode >= 300) return Array.Empty<Manga>();

        using var sr = new StreamReader(res.result);
        var html = sr.ReadToEnd();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var anchors = doc.DocumentNode.SelectNodes("//a[@href and contains(@href,'/manga/')]");
        if (anchors is null) return Array.Empty<Manga>();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<Manga>();

        foreach (var a in anchors)
        {
            var href = a.GetAttributeValue("href", null);
            if (string.IsNullOrWhiteSpace(href)) continue;

            var canonical = new Uri(baseUri, href).ToString();
            var ms = _seriesUrl.Match(canonical);
            if (!ms.Success) continue;

            var id = ms.Groups["id"].Value;
            var slug = ms.Groups["slug"].Value;
            var key = $"{id}/{slug}";
            if (!seen.Add(key)) continue;

            string title = slug.Replace('-', ' ');
            string cover = string.Empty;

            // preferisci sempre la pagina serie per titolo/cover corretti
            var seriesRes = downloadClient.MakeRequest(canonical, RequestType.MangaInfo);
            if ((int)seriesRes.statusCode >= 200 && (int)seriesRes.statusCode < 300)
            {
                using var srs = new StreamReader(seriesRes.result);
                var seriesHtml = srs.ReadToEnd();

                var sdoc = new HtmlDocument();
                sdoc.LoadHtml(seriesHtml);

                title =
                    sdoc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null)
                    ?? sdoc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim()
                    ?? title;

                title = CleanTitleSuffix(title);

                cover =
                    ExtractOgImage(seriesHtml, new Uri(canonical))
                    ?? sdoc.DocumentNode.SelectSingleNode("//div[contains(@class,'cover') or contains(@class,'poster')]//img[@src or @data-src]")?.GetAttributeValue("data-src", null)
                    ?? sdoc.DocumentNode.SelectSingleNode("//div[contains(@class,'cover') or contains(@class,'poster')]//img[@src or @data-src]")?.GetAttributeValue("src", null)
                    ?? string.Empty;

                if (!string.IsNullOrEmpty(cover))
                    cover = MakeAbsoluteUrl(new Uri(canonical), cover);
            }
            else
            {
                var fallbackTitle = HtmlEntity.DeEntitize(a.InnerText).Trim();
                if (!string.IsNullOrWhiteSpace(fallbackTitle)) title = fallbackTitle;
                title = CleanTitleSuffix(title);
                cover = TryExtractCoverFromSearchCard(a, baseUri);
            }

            list.Add(new Manga(
                $"{id}/{slug}",
                HtmlEntity.DeEntitize(title).Trim(),
                string.Empty,
                canonical,
                cover,
                MangaReleaseStatus.Unreleased,
                this,
                new List<Author>(),
                new List<MangaTag>(),
                new List<Link>(),
                new List<MangaAltTitle>(),
                year: null,
                originalLanguage: "it"
            ));
        }

        return list.ToArray();
    }

    // ======================== URL → Manga ===========================
    public override Manga? GetMangaFromUrl(string url)
    {
        var m = _seriesUrl.Match(url);
        if (!m.Success) return null;
        return GetMangaFromId($"{m.Groups["id"].Value}/{m.Groups["slug"].Value}");
    }

    // ======================== ID → Manga ============================
    public override Manga? GetMangaFromId(string mangaIdOnSite)
    {
        var parts = mangaIdOnSite.Split('/', 2);
        if (parts.Length != 2) return null;

        var id = parts[0];
        var slug = parts[1];

        var url = $"https://www.mangaworld.cx/manga/{id}/{slug}/";
        var res = downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)res.statusCode < 200 || (int)res.statusCode >= 300) return null;

        using var sr = new StreamReader(res.result);
        var html = sr.ReadToEnd();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var title =
            doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null)
            ?? doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim()
            ?? slug.Replace('-', ' ');

        title = CleanTitleSuffix(title);

        var cover =
            ExtractOgImage(html, new Uri(url))
            ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class,'cover') or contains(@class,'poster')]//img[@src or @data-src]")?.GetAttributeValue("data-src", null)
            ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class,'cover') or contains(@class,'poster')]//img[@src or @data-src]")?.GetAttributeValue("src", null)
            ?? string.Empty;

        if (!string.IsNullOrEmpty(cover))
            cover = MakeAbsoluteUrl(new Uri(url), cover);

        var description =
            doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", null)
            ?? HtmlEntity.DeEntitize(
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'description') or contains(@class,'trama')]")
                ?.InnerText ?? string.Empty
            ).Trim();

        return new Manga(
            mangaIdOnSite,
            HtmlEntity.DeEntitize(title),
            description,
            url,
            cover,
            MangaReleaseStatus.Unreleased,
            this,
            new List<Author>(),
            new List<MangaTag>(),
            new List<Link>(),
            new List<MangaAltTitle>(),
            year: null,
            originalLanguage: "it"
        );
    }

    // ========================== CAPITOLI ============================
    public override Chapter[] GetChapters(Manga manga, string? language = null)
    {
        var parts = manga.IdOnConnectorSite.Split('/', 2);
        if (parts.Length != 2) return Array.Empty<Chapter>();

        var id = parts[0];
        var slug = parts[1];
        var seriesUrl = $"https://www.mangaworld.cx/manga/{id}/{slug}/";

        string html = FetchHtmlWithFallback(seriesUrl, out var baseUri);
        if (string.IsNullOrEmpty(html)) return Array.Empty<Chapter>();

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var chapters = ParseChaptersFromHtml(manga, doc, baseUri);

        // Ordinamento finale: Volume → Capitolo (numerico)
        return chapters
            .OrderBy(c => c.VolumeNumber ?? 0)
            .ThenBy(c => TryParseDouble(c.ChapterNumber))
            .ToArray();
    }

    // ===================== IMMAGINI CAPITOLO =======================
    private static readonly Regex _imagesArray = new(@"images\s*=\s*\[(?<arr>.*?)\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex _urlInQuotes = new("\"(https?[^\"\\]]+)\"");
    internal override string[] GetChapterImageUrls(Chapter chapter)
    {
        var url = EnsureListStyle(chapter.Url);

        var res = downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)res.statusCode < 200 || (int)res.statusCode >= 300) return Array.Empty<string>();

        using var sr = new StreamReader(res.result);
        var html = sr.ReadToEnd();

        var baseUri = new Uri(url);

        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var imgs = doc.DocumentNode.SelectNodes("//img[@data-src or @src or @srcset]") ?? new HtmlNodeCollection(null);

        IEnumerable<string> fromDom = imgs
            .SelectMany(i =>
            {
                var list = new List<string>();
                var ds = i.GetAttributeValue("data-src", null);
                var s = i.GetAttributeValue("src", null);
                var ss = i.GetAttributeValue("srcset", null);

                if (!string.IsNullOrWhiteSpace(ds)) list.Add(ds);
                if (!string.IsNullOrWhiteSpace(s)) list.Add(s);
                if (!string.IsNullOrWhiteSpace(ss))
                {
                    foreach (var part in ss.Split(','))
                    {
                        var p = part.Trim().Split(' ')[0];
                        if (!string.IsNullOrWhiteSpace(p)) list.Add(p);
                    }
                }
                return list;
            })
            .Select(x => MakeAbsoluteUrl(baseUri, x))
            .Where(u =>
            {
                var z = u.ToLowerInvariant();
                return z.StartsWith("http") && (z.Contains(".jpg") || z.Contains(".jpeg") || z.Contains(".png") || z.Contains(".webp"));
            });

        var m = _imagesArray.Match(html);
        IEnumerable<string> fromJs = Enumerable.Empty<string>();
        if (m.Success)
        {
            var urls = _urlInQuotes.Matches(m.Groups["arr"].Value);
            fromJs = urls.Select(mm => MakeAbsoluteUrl(baseUri, mm.Groups[1].Value));
        }

        var final = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var u in fromDom.Concat(fromJs))
            if (seen.Add(u)) final.Add(u);

        return final.ToArray();
    }

    // ============================ PARSER CAPITOLI ===================
    private static readonly Regex RexVolume = new(@"[Vv]olume\s+([0-9]+)", RegexOptions.Compiled);
    private static readonly Regex RexChapter = new(@"(?:\b[Cc]apitolo|\b[Cc]h(?:apter)?)\s*([0-9]+(?:\.[0-9]+)?)", RegexOptions.Compiled);

    private List<Chapter> ParseChaptersFromHtml(Manga manga, HtmlDocument document, Uri baseUri)
    {
        var ret = new List<Chapter>();

        // wrapper principale
        var chaptersWrapper = document.DocumentNode.SelectSingleNode("//div[contains(@class,'chapters-wrapper')]");
        // layout A: volumi raggruppati
        var volumeElements = document.DocumentNode.SelectNodes("//div[contains(@class,'volume-element')]");

        if (volumeElements is not null && volumeElements.Count > 0)
        {
            foreach (var volNode in volumeElements)
            {
                // titolo volume, es. "<p>Volume 24</p>"
                var volText = volNode.SelectSingleNode(".//div[contains(@class,'volume')]/p")?.InnerText ?? string.Empty;

                int? volumeNumber = null;
                var vm = RexVolume.Match(volText);
                if (vm.Success && int.TryParse(vm.Groups[1].Value, out var volParsed))
                    volumeNumber = volParsed;

                // capitoli dentro il blocco volume
                var chapterNodes = volNode
                    .SelectSingleNode(".//div[contains(@class,'volume-chapters')]")
                    ?.SelectNodes(".//div") ?? new HtmlNodeCollection(null);

                foreach (var chNode in chapterNodes)
                {
                    var anchor = chNode.SelectSingleNode(".//a[@href]");
                    if (anchor is null) continue;

                    var spanText = anchor.SelectSingleNode(".//span")?.InnerText ?? anchor.InnerText ?? string.Empty;

                    var cm = RexChapter.Match(spanText);
                    if (!cm.Success) continue;

                    string chapterNumber = NormalizeNumber(cm.Groups[1].Value);
                    string href = anchor.GetAttributeValue("href", "");
                    if (string.IsNullOrWhiteSpace(href)) continue;

                    var rel = MakeAbsoluteUrl(baseUri, href);
                    var ensured = EnsureListStyle(EnsureReaderUrlHasPage(rel));

                    // title:null per evitare duplicazioni nel filename
                    ret.Add(new Chapter(manga, ensured, chapterNumber, volumeNumber, title: null));
                }
            }
        }
        else
        {
            // layout B: lista piatta (niente blocchi volume) → v1: Volume 0
            var chapterNodes = chaptersWrapper?.SelectNodes(".//div[contains(@class,'chapter')]")
                                ?? document.DocumentNode.SelectNodes("//div[contains(@class,'chapter')]")
                                ?? new HtmlNodeCollection(null);

            foreach (var chNode in chapterNodes)
            {
                var anchor = chNode.SelectSingleNode(".//a[@href]") ?? chNode.SelectSingleNode(".//a");
                if (anchor is null) continue;

                var spanText = anchor.SelectSingleNode(".//span")?.InnerText ?? anchor.InnerText ?? string.Empty;

                var cm = RexChapter.Match(spanText);
                if (!cm.Success) continue;

                string chapterNumber = NormalizeNumber(cm.Groups[1].Value);
                string href = anchor.GetAttributeValue("href", "");
                if (string.IsNullOrWhiteSpace(href)) continue;

                var rel = MakeAbsoluteUrl(baseUri, href);
                var ensured = EnsureListStyle(EnsureReaderUrlHasPage(rel));

                // v1 behaviour: senza volumi → Volume 0
                ret.Add(new Chapter(manga, ensured, chapterNumber, 0, title: null));
            }
        }

        return ret;
    }

    // ============================ HELPERS ===========================
    private static readonly Regex _seriesUrl = new(@"https?://[^/]+/manga/(?<id>\d+)/(?<slug>[^/]+)/?", RegexOptions.IgnoreCase);

    private string FetchHtmlWithFallback(string seriesUrl, out Uri baseUri)
    {
        baseUri = new Uri(seriesUrl);

        // 1) tenta client "Default"
        var res = downloadClient.MakeRequest(seriesUrl, RequestType.Default);
        if ((int)res.statusCode >= 200 && (int)res.statusCode < 300)
        {
            using var sr = new StreamReader(res.result);
            var html = sr.ReadToEnd();
            if (!LooksLikeChallenge(html)) return html;
        }

        // 2) fallback: client “MangaInfo” (proxy/Flare se configurato)
        var res2 = downloadClient.MakeRequest(seriesUrl, RequestType.MangaInfo);
        if ((int)res2.statusCode >= 200 && (int)res2.statusCode < 300)
        {
            using var sr2 = new StreamReader(res2.result);
            return sr2.ReadToEnd();
        }

        return string.Empty;
    }

    private static bool LooksLikeChallenge(string html)
    {
        if (string.IsNullOrEmpty(html)) return true;
        var h = html.ToLowerInvariant();
        return h.Contains("cf-challenge") ||
               h.Contains("cf-browser-verification") ||
               h.Contains("just a moment") ||
               h.Contains("verify you are human") ||
               h.Contains("captcha");
    }

    private static string EnsureReaderUrlHasPage(string url)
    {
        var u = url ?? string.Empty;
        var m = Regex.Match(u, @"(/read/[0-9a-fA-F]{16,64})(/(\d+))?", RegexOptions.IgnoreCase);
        if (m.Success && string.IsNullOrEmpty(m.Groups[2].Value))
        {
            var qIdx = u.IndexOf('?', StringComparison.Ordinal);
            if (qIdx >= 0) u = u.Insert(qIdx, "/1");
            else u = u.TrimEnd('/') + "/1";
        }
        return u;
    }

    private static string EnsureListStyle(string url)
    {
        if (string.IsNullOrEmpty(url)) return url;
        if (url.Contains("style=list", StringComparison.OrdinalIgnoreCase)) return url;
        return url.Contains('?') ? (url + "&style=list") : (url + "?style=list");
    }

    private static string NormalizeNumber(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return "0";
        s = s.Trim();
        var m = Regex.Match(s, @"^\s*0*(\d+)(?:\.(\d+))?\s*$");
        if (!m.Success) return s;
        var intPart = m.Groups[1].Value.TrimStart('0');
        if (intPart.Length == 0) intPart = "0";
        var frac = m.Groups[2].Success ? "." + m.Groups[2].Value : "";
        return intPart + frac;
    }

    private static double TryParseDouble(string s)
        => double.TryParse(s.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var d) ? d : double.MaxValue;

    private static string MakeAbsoluteUrl(Uri baseUri, string s)
    {
        s = s.Trim();
        if (s.StartsWith("//")) return "https:" + s;
        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            s.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) return s;
        if (s.StartsWith("/")) return new Uri(baseUri, s).ToString();
        return new Uri(baseUri, s).ToString();
    }

    private static string TryExtractCoverFromSearchCard(HtmlNode linkNode, Uri baseUri)
    {
        var container = linkNode.Ancestors("div")
            .FirstOrDefault(div =>
            {
                var cls = div.GetAttributeValue("class", "");
                return cls.Contains("card") || cls.Contains("manga") || cls.Contains("item") || cls.Contains("poster") || cls.Contains("thumb");
            });

        var img = container?.SelectSingleNode(".//img[@data-src or @src]");
        if (img is null) return string.Empty;

        var raw = img.GetAttributeValue("data-src", null) ?? img.GetAttributeValue("src", null);
        return string.IsNullOrWhiteSpace(raw) ? string.Empty : MakeAbsoluteUrl(baseUri, raw!);
    }

    private static string? ExtractOgImage(string html, Uri baseUri)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var og = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", null);
        return string.IsNullOrWhiteSpace(og) ? null : MakeAbsoluteUrl(baseUri, og!);
    }

    // ===================== TITLE CLEANUP (suffisso MW) ==============
    private static readonly Regex _mwSuffix = new(@"\s*(Scan\s\w+\s-\sMangaWorld)$", RegexOptions.IgnoreCase);

    private static string CleanTitleSuffix(string? t)
    {
        if (string.IsNullOrWhiteSpace(t)) return t ?? string.Empty;
        return _mwSuffix.Replace(t, "").Trim();
    }
}

