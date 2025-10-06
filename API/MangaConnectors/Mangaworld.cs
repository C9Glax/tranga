using System.Text.RegularExpressions;
using System.Web;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using HtmlAgilityPack;
// ReSharper disable StringLiteralTypo

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

    // ============================ SEARCH ============================
    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        Uri baseUri = new ("https://www.mangaworld.cx/");
        Uri searchUrl = new (baseUri, "archive?keyword=" + HttpUtility.UrlEncode(mangaSearchName));

        HttpResponseMessage res = downloadClient.MakeRequest(searchUrl.ToString(), RequestType.Default).Result;
        if ((int)res.StatusCode < 200 || (int)res.StatusCode >= 300)
            return [];

        using StreamReader sr = new (res.Content.ReadAsStream());
        string html = sr.ReadToEnd();

        HtmlDocument doc = new ();
        doc.LoadHtml(html);

        HtmlNodeCollection? anchors = doc.DocumentNode.SelectNodes("//a[@href and contains(@href,'/manga/')]");
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract Apparently it does return null. Ask AgilityPack why the return type isnt marked as such...
        if (anchors is null || anchors.Count < 1)
            return [];

        List<(Manga, MangaConnectorId<Manga>)> list = [];

        foreach (HtmlNode a in anchors)
        {
            string href = a.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(href))
                continue;

            string canonical = new Uri(baseUri, href).ToString();

            (Manga, MangaConnectorId<Manga>)? manga = GetMangaFromUrl(canonical);
            if(manga is null)
                continue;

            list.Add(((Manga, MangaConnectorId<Manga>))manga);
        }

        return list.ToArray();
    }

    // ======================== URL → Manga ===========================
    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
        Match m = SeriesUrl.Match(url);
        if (!m.Success)
            return null;
        return GetMangaFromId($"{m.Groups["id"].Value}/{m.Groups["slug"].Value}");
    }

    // ======================== ID → Manga ============================
    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
        string[] parts = mangaIdOnSite.Split('/', 2);
        if (parts.Length != 2)
            return null;

        string id = parts[0];
        string slug = parts[1];

        string url = $"https://www.mangaworld.cx/manga/{id}/{slug}/";
        HttpResponseMessage res = downloadClient.MakeRequest(url, RequestType.MangaInfo).Result;
        if ((int)res.StatusCode < 200 || (int)res.StatusCode >= 300)
            return null;

        using StreamReader sr = new (res.Content.ReadAsStream());
        string html = sr.ReadToEnd();

        HtmlDocument doc = new ();
        doc.LoadHtml(html);

        string title =
            doc.DocumentNode.SelectSingleNode("//meta[@property='og:title']")?.GetAttributeValue("content", null)
            ?? doc.DocumentNode.SelectSingleNode("//h1")?.InnerText?.Trim()
            ?? slug.Replace('-', ' ');

        title = CleanTitleSuffix(title);

        string cover =
            ExtractOgImage(html, new Uri(url))
            ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class,'cover') or contains(@class,'poster')]//img[@src or @data-src]")?.GetAttributeValue("data-src", null)
            ?? doc.DocumentNode.SelectSingleNode("//div[contains(@class,'cover') or contains(@class,'poster')]//img[@src or @data-src]")?.GetAttributeValue("src", null)
            ?? string.Empty;

        if (!string.IsNullOrEmpty(cover))
            cover = MakeAbsoluteUrl(new Uri(url), cover);

        string description =
            doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?.GetAttributeValue("content", null)
            ?? HtmlEntity.DeEntitize(
                doc.DocumentNode.SelectSingleNode("//div[contains(@class,'description') or contains(@class,'trama')]")
                ?.InnerText ?? string.Empty
            ).Trim();

        // === STATO (scheda dettaglio) ===
        MangaReleaseStatus status = MangaReleaseStatus.Unreleased;
        string? detailRawStatus = ExtractItalianStatus(doc);
        if (!string.IsNullOrWhiteSpace(detailRawStatus))
            status = MapItalianStatus(detailRawStatus);
        
        Manga m = new (
            HtmlEntity.DeEntitize(title).Trim(), 
            description,
            cover,
            status, 
            [], 
            [], 
            [], 
            [],
            originalLanguage: "it");
        MangaConnectorId<Manga> mcId = new (m, 
            this, 
            $"{id}/{slug}", 
            $"https://www.mangaworld.cx/manga/{id}/{slug}/");
        m.MangaConnectorIds.Add(mcId);
        return (m, mcId);
    }

    // ========================== CAPITOLI ============================
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

        HtmlDocument doc = new ();
        doc.LoadHtml(html);

        List<(Chapter, MangaConnectorId<Chapter>)> chapters = ParseChaptersFromHtml(mangaId.Obj ,doc, baseUri);

        // Ordinamento finale: Volume → Capitolo (numerico)
        return chapters
            .OrderBy(c => c.Item1, new Chapter.ChapterComparer())
            .ToArray();
    }

    // ===================== IMMAGINI CAPITOLO =======================
    private static readonly Regex ImagesArray = new(@"images\s*=\s*\[(?<arr>.*?)\]", RegexOptions.Singleline | RegexOptions.IgnoreCase);
    private static readonly Regex UrlInQuotes = new("\"(https?[^\"\\]]+)\"");
    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        string url = EnsureListStyle(chapterId.WebsiteUrl ?? $"https://www.mangaworld.cx/manga/{chapterId.IdOnConnectorSite}");

        HttpResponseMessage res = downloadClient.MakeRequest(url, RequestType.MangaInfo).Result;
        if ((int)res.StatusCode < 200 || (int)res.StatusCode >= 300)
            return [];

        using StreamReader sr = new (res.Content.ReadAsStream());
        string html = sr.ReadToEnd();

        Uri baseUri = new (url);

        HtmlDocument doc = new ();
        doc.LoadHtml(html);

        HtmlNodeCollection imageNodes = doc.DocumentNode.SelectNodes("//img[@data-src or @src or @srcset]") ?? new HtmlNodeCollection(null);

        IEnumerable<string> fromDom = imageNodes
            .SelectMany(i =>
            {
                var list = new List<string>();
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
            .Where(u =>
            {
                string z = u.ToLowerInvariant();
                return z.StartsWith("http") && (z.Contains(".jpg") || z.Contains(".jpeg") || z.Contains(".png") || z.Contains(".webp"));
            });

        Match m = ImagesArray.Match(html);
        IEnumerable<string> fromJs = [];
        if (m.Success)
        {
            MatchCollection urls = UrlInQuotes.Matches(m.Groups["arr"].Value);
            fromJs = urls.Select(mm => MakeAbsoluteUrl(baseUri, mm.Groups[1].Value));
        }

        List<string> final = new ();
        HashSet<string> seen = new (StringComparer.OrdinalIgnoreCase);
        foreach (string u in fromDom.Concat(fromJs))
            if (seen.Add(u))
                final.Add(u);

        return final.ToArray();
    }

    // ============================ PARSER CAPITOLI ===================
    private static readonly Regex RexVolume = new(@"[Vv]olume\s+([0-9]+)", RegexOptions.Compiled);
    private static readonly Regex RexChapter = new(@"(?:\b[Cc]apitolo|\b[Cc]h(?:apter)?)\s*([0-9]+(?:\.[0-9]+)?)", RegexOptions.Compiled);
    private static readonly Regex RexChapterId = new(@"manga\/([0-9]+\/[a-z0-9\-]+\/read\/[a-z0-9]+)\/", RegexOptions.Compiled);

    private List<(Chapter, MangaConnectorId<Chapter>)> ParseChaptersFromHtml(Manga manga, HtmlDocument document, Uri baseUri)
    {
        List<(Chapter, MangaConnectorId<Chapter>)> ret = new ();

        // wrapper principale
        HtmlNode? chaptersWrapper = document.DocumentNode.SelectSingleNode("//div[contains(@class,'chapters-wrapper')]");
        // layout A: volumi raggruppati
        HtmlNodeCollection? volumeElements = document.DocumentNode.SelectNodes("//div[contains(@class,'volume-element')]");

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (volumeElements is not null && volumeElements.Count > 0)
        {
            foreach (HtmlNode volNode in volumeElements)
            {
                // titolo volume, es. "<p>Volume 24</p>"
                string volText = volNode.SelectSingleNode(".//div[contains(@class,'volume')]/p")?.InnerText ?? string.Empty;

                int? volumeNumber = null;
                Match vm = RexVolume.Match(volText);
                if (vm.Success && int.TryParse(vm.Groups[1].Value, out int volParsed))
                    volumeNumber = volParsed;

                // capitoli dentro il blocco volume
                HtmlNodeCollection chapterNodes = volNode
                    .SelectSingleNode(".//div[contains(@class,'volume-chapters')]")
                    ?.SelectNodes(".//div") ?? new HtmlNodeCollection(null);

                foreach (HtmlNode chNode in chapterNodes)
                {
                    HtmlNode? anchor = chNode.SelectSingleNode(".//a[@href]");
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
                    if (anchor is null)
                        continue;

                    string spanText = anchor.SelectSingleNode(".//span")?.InnerText ?? anchor.InnerText ?? string.Empty;

                    Match cm = RexChapter.Match(spanText);
                    if (!cm.Success)
                        continue;

                    string chapterNumber = NormalizeNumber(cm.Groups[1].Value);
                    string href = anchor.GetAttributeValue("href", "");
                    if (string.IsNullOrWhiteSpace(href))
                        continue;

                    string rel = MakeAbsoluteUrl(baseUri, href);
                    string ensured = EnsureListStyle(EnsureReaderUrlHasPage(rel));

                    Match idMatch = RexChapterId.Match(ensured);
                    if(!idMatch.Success)
                        continue;
                    string id = idMatch.Groups[1].Value;

                    Chapter chapter = new (manga, chapterNumber, volumeNumber);
                    MangaConnectorId<Chapter> chId = new(chapter, this, id, ensured);
                    chapter.MangaConnectorIds.Add(chId);

                    // title:null per evitare duplicazioni nel filename
                    ret.Add((chapter, chId));
                }
            }
        }
        else
        {
            // layout B: lista piatta (niente blocchi volume) → v1: Volume 0
            HtmlNodeCollection chapterNodes = chaptersWrapper?.SelectNodes(".//div[contains(@class,'chapter')]")
                                              ?? document.DocumentNode.SelectNodes("//div[contains(@class,'chapter')]")
                                              ?? new HtmlNodeCollection(null);

            foreach (HtmlNode chNode in chapterNodes)
            {
                HtmlNode? anchor = chNode.SelectSingleNode(".//a[@href]") ?? chNode.SelectSingleNode(".//a");
                if (anchor is null)
                    continue;

                string spanText = anchor.SelectSingleNode(".//span")?.InnerText ?? anchor.InnerText ?? string.Empty;

                Match cm = RexChapter.Match(spanText);
                if (!cm.Success)
                    continue;

                string chapterNumber = NormalizeNumber(cm.Groups[1].Value);
                string href = anchor.GetAttributeValue("href", "");
                if (string.IsNullOrWhiteSpace(href))
                    continue;

                string rel = MakeAbsoluteUrl(baseUri, href);
                string ensured = EnsureListStyle(EnsureReaderUrlHasPage(rel));

                Match idMatch = RexChapterId.Match(ensured);
                if(!idMatch.Success)
                    continue;
                string id = idMatch.Groups[1].Value;

                // v1 behaviour: senza volumi → Volume 0
                Chapter chapter = new (manga, chapterNumber, null);
                MangaConnectorId<Chapter> chId = new(chapter, this, id, ensured);

                ret.Add((chapter, chId));
            }
        }

        return ret;
    }

    // ============================ HELPERS ===========================
    private static readonly Regex SeriesUrl = new(@"https?://[^/]+/manga/(?<id>\d+)/(?<slug>[^/]+)/?", RegexOptions.IgnoreCase);

    private string FetchHtmlWithFallback(string seriesUrl, out Uri baseUri)
    {
        baseUri = new (seriesUrl);

        // 1) tenta client "Default"
        HttpResponseMessage res = downloadClient.MakeRequest(seriesUrl, RequestType.Default).Result;
        if ((int)res.StatusCode >= 200 && (int)res.StatusCode < 300)
        {
            using StreamReader sr = new (res.Content.ReadAsStream());
            string html = sr.ReadToEnd();
            if (!LooksLikeChallenge(html))
                return html;
        }

        // 2) fallback: client “MangaInfo” (proxy/Flare se configurato)
        HttpResponseMessage res2 = downloadClient.MakeRequest(seriesUrl, RequestType.MangaInfo).Result;
        if ((int)res2.StatusCode >= 200 && (int)res2.StatusCode < 300)
        {
            using StreamReader sr2 = new StreamReader(res2.Content.ReadAsStream());
            return sr2.ReadToEnd();
        }

        return string.Empty;
    }

    private static bool LooksLikeChallenge(string html)
    {
        if (string.IsNullOrEmpty(html)) return true;
        string h = html.ToLowerInvariant();
        return h.Contains("cf-challenge") ||
               h.Contains("cf-browser-verification") ||
               h.Contains("just a moment") ||
               h.Contains("verify you are human") ||
               h.Contains("captcha");
    }

    private static string EnsureReaderUrlHasPage(string url)
    {
        Match m = Regex.Match(url, @"(/read/[0-9a-fA-F]{16,64})(/(\d+))?", RegexOptions.IgnoreCase);
        if (m.Success && string.IsNullOrEmpty(m.Groups[2].Value))
        {
            int qIdx = url.IndexOf('?', StringComparison.Ordinal);
            if (qIdx >= 0)
                url = url.Insert(qIdx, "/1");
            else
                url = url.TrimEnd('/') + "/1";
        }
        return url;
    }

    private static string EnsureListStyle(string url)
    {
        if (string.IsNullOrEmpty(url))
            return url;
        if (url.Contains("style=list", StringComparison.OrdinalIgnoreCase))
            return url;
        return url.Contains('?') ? (url + "&style=list") : (url + "?style=list");
    }

    private static string NormalizeNumber(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "0";
        s = s.Trim();
        Match m = Regex.Match(s, @"^\s*0*(\d+)(?:\.(\d+))?\s*$");
        if (!m.Success)
            return s;
        string intPart = m.Groups[1].Value.TrimStart('0');
        if (intPart.Length == 0)
            intPart = "0";
        string frac = m.Groups[2].Success
            ? "." + m.Groups[2].Value
            : "";
        return intPart + frac;
    }

    private static string MakeAbsoluteUrl(Uri baseUri, string s)
    {
        s = s.Trim();
        if (s.StartsWith("//"))
            return "https:" + s;
        if (s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            s.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            return s;
        if (s.StartsWith("/"))
            return new Uri(baseUri, s).ToString();
        return new Uri(baseUri, s).ToString();
    }

    private static string? ExtractOgImage(string html, Uri baseUri)
    {
        HtmlDocument doc = new ();
        doc.LoadHtml(html);
        string? og = doc.DocumentNode.SelectSingleNode("//meta[@property='og:image']")?.GetAttributeValue("content", null);
        return string.IsNullOrWhiteSpace(og) ? null : MakeAbsoluteUrl(baseUri, og!);
    }

    // ===================== TITLE CLEANUP (suffisso MW) ==============
    private static readonly Regex MwSuffix = new(@"\s*(Scan\s\w+\s-\sMangaWorld)$", RegexOptions.IgnoreCase);

    private static string CleanTitleSuffix(string? t)
    {
        if (string.IsNullOrWhiteSpace(t))
            return t ?? string.Empty;
        return MwSuffix.Replace(t, "").Trim();
    }

    // ===================== STATO (estrazione + mapping) =============
    private static string? ExtractItalianStatus(HtmlDocument doc)
    {
        // 1) Percorso più comune: "Stato: <valore>"
        HtmlNode? node = doc.DocumentNode.SelectSingleNode("//span[normalize-space(text())='Stato:']/following-sibling::*[1]")
                         ?? doc.DocumentNode.SelectSingleNode("//span[contains(translate(., 'STATO', 'stato'), 'stato')]/following-sibling::*[1]");
        string? val = node?.InnerText?.Trim();
        if (!string.IsNullOrWhiteSpace(val)) return HtmlEntity.DeEntitize(val);

        // 2) Blocchi info vari (tollerante a cambi DOM)
        HtmlNodeCollection? blocks = doc.DocumentNode.SelectNodes("//*[contains(@class,'info') or contains(@class,'details') or contains(@class,'meta') or contains(@class,'attributes') or contains(@class,'list-group')]");
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (blocks is not null)
        {
            foreach (HtmlNode block in blocks)
            {
                HtmlNodeCollection labels = block.SelectNodes(".//dt|.//li|.//div|.//span|.//strong") ?? new HtmlNodeCollection(null);
                foreach (HtmlNode label in labels)
                {
                    string? t = label.InnerText?.Trim()?.ToLowerInvariant();
                    if (string.IsNullOrEmpty(t))
                        continue;
                    if (t != "stato" && t != "stato:" && !t.Contains("stato"))
                        continue;
                    string? vv = label.SelectSingleNode("./following-sibling::*[1]")?.InnerText?.Trim()
                                 ?? label.ParentNode?.SelectSingleNode(".//a|.//span|.//strong")?.InnerText?.Trim();
                    if (!string.IsNullOrWhiteSpace(vv))
                        return HtmlEntity.DeEntitize(vv);
                }
            }
        }

        // 3) Fallback testuale grezzo
        string body = doc.DocumentNode.InnerText;
        Match m = Regex.Match(body, @"Stato\s*:\s*([A-Za-zÀ-ÿ\s\-]+)", RegexOptions.IgnoreCase);
        return m.Success
            ? m.Groups[1].Value.Trim()
            : null;
    }

    private static MangaReleaseStatus MapItalianStatus(string s) => s.Trim().ToLowerInvariant() switch
    {
        "in corso" or "ongoing" or "attivo" => MangaReleaseStatus.Continuing,
        "completo" or "concluso" or "finito" or "terminato" or "completed" => MangaReleaseStatus.Completed,
        "in pausa" or "pausa" or "hiatus" or "sospeso" => MangaReleaseStatus.OnHiatus,
        "droppato" or "cancellato" or "abbandonato" or "cancelled" or "interrotto" => MangaReleaseStatus.Cancelled,
        _ => MangaReleaseStatus.Unreleased
    };
}

