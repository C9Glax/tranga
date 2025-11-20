using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using log4net;
using System.Collections.Generic;
using System.Linq; // For OrderBy

namespace API.MangaConnectors;

public class AsuraComic : MangaConnector
{
    // Change: Use 'new' to hide base Log
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE1006:Naming Styles")]
    new protected static readonly ILog Log = LogManager.GetLogger(typeof(AsuraComic));

    public AsuraComic() : base("AsuraComic", ["en"], ["asuracomic.net"], "https://asuracomic.net/favicon.ico")
    {
        this.downloadClient = new HttpDownloadClient(); // Use Http for all (no RequestResult)
    }

    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        Log.Info($"Searching: {mangaSearchName}");
        string sanitizedTitle = string.Join(' ', Regex.Matches(mangaSearchName, @"[A-Za-z]+").Where(m => m.Value.Length > 0)).ToLowerInvariant();
        string requestUrl = $"https://asuracomic.net/series?name={HttpUtility.UrlEncode(sanitizedTitle)}";
        HttpResponseMessage response = downloadClient.MakeRequest(requestUrl, RequestType.Default).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Request failed or no HTML retrieved");
            return [];
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Log.Debug($"Search HTML length: {html.Length}");
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        HtmlNodeCollection? nodes = doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'series/')]"); // Match v1 XPath
        Log.Debug($"Found {nodes?.Count ?? 0} series nodes in search HTML");
        if (nodes is null || nodes.Count < 1)
        {
            Log.Error("No series links found");
            return [];
        }

        var seenUrls = new HashSet<string>(); // Dedup URLs
        List<(Manga, MangaConnectorId<Manga>)> mangas = new();
        foreach (HtmlNode node in nodes)
        {
            string href = node.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(href))
            {
                // Fix: Ensure leading / for relative hrefs (match v1 TrimStart logic but add / if missing)
                if (!href.StartsWith("/"))
                    href = "/" + href;
                string fullUrl = $"https://asuracomic.net{href}";
                if (seenUrls.Add(fullUrl))
                {
                    Log.Debug($"Fetching from {fullUrl}"); // Debug URL
                    var manga = GetMangaFromUrl(fullUrl);
                    if (manga.HasValue)
                    {
                        mangas.Add(manga.Value);
                        Log.Debug($"Added manga from {fullUrl}");
                    }
                    else
                    {
                        Log.Warn($"Failed to parse manga from {fullUrl}"); // Debug fails
                    }
                }
            }
        }

        Log.Info($"Search '{mangaSearchName}' yielded {mangas.Count} results.");
        return mangas.DistinctBy(r => r.Item1.Key).ToArray(); // Dedup by manga Key
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
        Log.Info($"Fetching manga from URL: {url}");
        Match urlMatch = Regex.Match(url, @"https?://(?:www\.)?asuracomic\.net/series/(?<id>[^/]+)");
        if (!urlMatch.Success)
            return null;

        string id = urlMatch.Groups["id"].Value;
        // Fetch once using url (no double fetch)
        HttpResponseMessage response = downloadClient.MakeRequest(url, RequestType.MangaInfo).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to retrieve manga page");
            return null;
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        return ParseMangaFromHtml(doc, id, url);
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
        string url = $"https://asuracomic.net/series/{mangaIdOnSite}";
        HttpResponseMessage response = downloadClient.MakeRequest(url, RequestType.MangaInfo).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to retrieve manga page");
            return null;
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        return ParseMangaFromHtml(doc, mangaIdOnSite, url); // Use full slug as ID
    }

    private (Manga, MangaConnectorId<Manga>) ParseMangaFromHtml(HtmlDocument doc, string mangaId, string url)
    {
        // Title with cleanup (kept for robustness, but simple decode to match original)
        HtmlNode? titleNode = doc.DocumentNode.SelectSingleNode("//title");
        string rawTitle = Regex.Match(titleNode?.InnerText ?? mangaId, @"(.*) - Asura Scans").Groups[1].Value.Trim();
        string cleanTitle = HtmlEntity.DeEntitize(rawTitle).Trim(); // Simple decode like original

        // Cover
        HtmlNode? coverNode = doc.DocumentNode.SelectSingleNode("//img[@alt='poster']");
        string coverUrl = coverNode?.GetAttributeValue("src", "") ?? "";
        if (!string.IsNullOrEmpty(coverUrl) && !coverUrl.StartsWith("http"))
            coverUrl = $"https://asuracomic.net{coverUrl}";

        // Description
        HtmlNode? descNode = doc.DocumentNode.SelectSingleNode("//h3[starts-with(text(),'Synopsis')]/../span");
        string description = HtmlEntity.DeEntitize(descNode?.InnerText ?? "").Trim();

        // Tags
        HtmlNodeCollection? genreNodes = doc.DocumentNode.SelectNodes("//h3[text()='Genres']/../div/button");
        List<MangaTag> tags = genreNodes?.Select(b => new MangaTag(HtmlEntity.DeEntitize(b.InnerText.Trim()))).ToList() ?? [];

        // Status
        HtmlNode? statusNode = doc.DocumentNode.SelectSingleNode("//h3[text()='Status']/../h3[2]");
        string rawStatus = HtmlEntity.DeEntitize(statusNode?.InnerText ?? "").ToLowerInvariant().Trim();
        MangaReleaseStatus releaseStatus = rawStatus switch
        {
            "ongoing" or "season end" => MangaReleaseStatus.Continuing,
            "hiatus" => MangaReleaseStatus.OnHiatus,
            "completed" => MangaReleaseStatus.Completed,
            "dropped" => MangaReleaseStatus.Cancelled,
            _ => MangaReleaseStatus.Unreleased
        };

        // Authors/Artists
        HtmlNodeCollection? authorNodes = doc.DocumentNode.SelectNodes("//h3[text()='Author']/../h3[not(text()='Author' or text()='_')]");
        HtmlNodeCollection? artistNodes = doc.DocumentNode.SelectNodes("//h3[text()='Artist']/../h3[not(text()='Artist' or text()='_')]");
        List<Author> authors = authorNodes?.Select(a => new Author(HtmlEntity.DeEntitize(a.InnerText.Trim()))).ToList() ?? [];
        if (artistNodes is not null)
            authors.AddRange(artistNodes.Select(a => new Author(HtmlEntity.DeEntitize(a.InnerText.Trim()))));

        // Year
        HtmlNode? firstChapterNode = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'chapter/1')]/../following-sibling::h3");
        uint? year = null;
        if (firstChapterNode?.InnerText is { } firstText && firstText.Contains(" "))
        {
            string datePart = firstText.Split(' ').Last();
            uint.TryParse(datePart, out uint parsedYear);
            year = parsedYear > 0 ? parsedYear : null;
        }

        List<AltTitle> altTitles = new();
        List<Link> links = new();
        // Match original constructor (null language for consistent Key)
        Manga manga = new(cleanTitle, description, coverUrl, releaseStatus, authors, tags, links, altTitles, null, 0f, year, null);
        
        // Workaround for Tranga merge duplicate Key: Append unique suffix to IdOnConnectorSite (e.g., timestamp hash)
        // This ensures each search creates a unique MangaConnectorId.Key, preventing EF conflict on add during merge.
        // Multiple IDs per manga/connector are harmless (allows URL variations if site changes).
        string uniqueSuffix = (DateTime.UtcNow.Ticks % 1000000).ToString("D6"); // 6-digit unique per ~10s
        string uniqueId = $"{mangaId}-{uniqueSuffix}";
        
        MangaConnectorId<Manga> mcId = new(manga, this, uniqueId, url);
        manga.MangaConnectorIds.Add(mcId);
        
        return (manga, mcId);
    }

    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> manga, string? language = null)
    {
        Log.Info($"Fetching chapters for: {manga.IdOnConnectorSite}");
        // Fix: Strip unique suffix for base slug (truncate after last '-')
        string baseSlug = manga.IdOnConnectorSite.Contains('-') ? manga.IdOnConnectorSite[..manga.IdOnConnectorSite.LastIndexOf('-')] : manga.IdOnConnectorSite;
        string websiteUrl = manga.WebsiteUrl ?? $"https://asuracomic.net/series/{baseSlug}";
        HttpResponseMessage response = downloadClient.MakeRequest(websiteUrl, RequestType.Default).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to load chapters page");
            return [];
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        HtmlNodeCollection? chapterNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/chapter/')]");
        if (chapterNodes is null)
            return [];

        var seenHrefs = new HashSet<string>();
        List<(Chapter, MangaConnectorId<Chapter>)> chapters = new();
        foreach (HtmlNode chapterNode in chapterNodes)
        {
            string href = chapterNode.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(href) || !seenHrefs.Add(href))
                continue;

            Log.Debug($"Raw href: '{href}', baseSlug: '{baseSlug}'");  // Debug: Check logs for patterns

            string fullUrl;
            if (href.StartsWith("http"))
            {
                fullUrl = href;
            }
            else if (href.StartsWith("/series/"))
            {
                fullUrl = $"https://asuracomic.net{href}";
            }
            else
            {
                // Fix: Normalize href (ensure leading /)
                if (!href.StartsWith("/"))
                    href = "/" + href;

                // Check if href already includes baseSlug + /chapter/ (e.g., "/slug/chapter/N")
                if (href.StartsWith($"/{baseSlug}/chapter/"))
                {
                    // Already has slug; prepend only /series
                    fullUrl = $"https://asuracomic.net/series{href}";
                }
                else if (Regex.Match(href, @"^/[^/]+/chapter/").Success)
                {
                    // Has some slug + /chapter/ but not matching baseSlug (site variation); prepend /series and replace mismatched slug with baseSlug
                    string mismatchedSlug = href.Substring(1, href.IndexOf('/', 1) - 1);  // Extract first segment after /
                    fullUrl = $"https://asuracomic.net/series/{baseSlug}{href.Substring(href.IndexOf('/', 1))}";
                }
                else
                {
                    // No slug; prepend full /series/{baseSlug}
                    fullUrl = $"https://asuracomic.net/series/{baseSlug}{href}";
                }
            }

            Log.Debug($"Constructed fullUrl: '{fullUrl}'");  // Debug: Verify output

            Match match = Regex.Match(chapterNode.InnerText.Trim(), @"Chapter\s+([0-9]+(?:\.[0-9]+)?)(?:\s*(.*))?", RegexOptions.IgnoreCase);
            if (!match.Success)
                continue;

            string chapterNumber = match.Groups[1].Value;
            string? chapterTitle = match.Groups[2].Success && !string.IsNullOrWhiteSpace(match.Groups[2].Value) ? match.Groups[2].Value.Trim() : null;

            Chapter ch = new(manga.Obj, chapterNumber, null, chapterTitle);
            string chapterId = href.StartsWith("/series/") ? href.Substring("/series/".Length) : href;
            MangaConnectorId<Chapter> mcId = new(ch, this, chapterId, fullUrl);
            ch.MangaConnectorIds.Add(mcId);
            chapters.Add((ch, mcId));
        }

        Log.Info($"Found {chapters.Count} chapters for {manga.Obj.Name}");
        return chapters.OrderBy(c => c.Item1, new Chapter.ChapterComparer()).ToArray();
    }

    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        Log.Info($"Getting Chapter Image-Urls: {chapterId.Obj}");
        if (chapterId.WebsiteUrl is null)
        {
            Log.Error("Chapter URL is null");
            return [];
        }

        string? referrer = null;
        if (chapterId.Obj.ParentManga.MangaConnectorIds is not null && chapterId.Obj.ParentManga.MangaConnectorIds.Any())
        {
            referrer = chapterId.Obj.ParentManga.MangaConnectorIds
                .FirstOrDefault(id => id.MangaConnectorName == this.Name)?.WebsiteUrl;
        }

        using var chromium = new ChromiumDownloadClient();
        HttpResponseMessage response = chromium.MakeRequest(chapterId.WebsiteUrl!, RequestType.Default, referrer).GetAwaiter().GetResult();

        if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
        {
            Log.Error("Failed to load chapter page with Chromium");
            return [];
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        Log.Debug($"HTML snippet (first 500 chars): {doc.DocumentNode.OuterHtml.Substring(0, Math.Min(500, doc.DocumentNode.OuterHtml.Length))}");

        HtmlNodeCollection? imageNodes = doc.DocumentNode.SelectNodes("//img[contains(@alt, 'chapter page') or contains(@alt, 'Chapter') or @class='page-break']");
        if (imageNodes is null || imageNodes.Count == 0)
        {
            Log.Warn("No chapter page images found");
            return [];
        }

        var imageUrls = imageNodes
            .Select(i => 
            {
                string src = i.GetAttributeValue("src", "");
                if (string.IsNullOrEmpty(src))
                    src = i.GetAttributeValue("data-src", "");
                if (!src.StartsWith("http"))
                    src = "https://asuracomic.net" + src;
                return src;
            })
            .Where(u => !string.IsNullOrEmpty(u))
            .ToArray();

        Log.Info($"Found {imageUrls.Length} images for chapter {chapterId.Obj}");
        return imageUrls;
    }

    private static string MakeAbsoluteUrl(Uri baseUri, string relativeOrAbsolute)
    {
        if (string.IsNullOrWhiteSpace(relativeOrAbsolute))
            return "";
        if (relativeOrAbsolute.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            return relativeOrAbsolute;
        return new Uri(baseUri, relativeOrAbsolute).ToString();
    }

    private static bool IsValidImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        string lowerUrl = url.ToLowerInvariant();
        return lowerUrl.Contains(".jpg") || lowerUrl.Contains(".jpeg") || lowerUrl.Contains(".png") || lowerUrl.Contains(".webp");
    }
}