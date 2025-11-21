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

    public AsuraComic() : base("AsuraComic", ["en"], ["asuracomic.net"], "https://asuracomic.net/images/logo.webp")
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
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        HtmlNodeCollection? nodes = doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'series/')]"); // Match v1 XPath
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
        Regex chapterIdRegex = new(@"\/chapter\/([0-9]+(?:\.[0-9]+)?)");
        List<(Chapter, MangaConnectorId<Chapter>)> chapters = new();

        var baseUri = new Uri(manga.WebsiteUrl);
        foreach (HtmlNode chapterNode in chapterNodes)
        {
            // Filter out premium chapters: Skip if anchor contains SVG (premium badge icon)
            if (chapterNode.Descendants("svg").Any())
            {
                Log.Debug($"Skipping premium chapter (SVG icon detected): {chapterNode.InnerText.Trim()}");
                continue;
            }

            string text = chapterNode.InnerText.Trim();
            string href = chapterNode.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(href)) continue;

            // Build full URL correctly using Uri resolution (handles relative/absolute)
            string url = new Uri(baseUri, href).ToString();
            Log.Debug($"Generated chapter URL: {url} from href: {href}");  // Debug log (remove after test)

            var chIdMatch = chapterIdRegex.Match(href);
            if (!chIdMatch.Success) continue;

            string chapterId = chIdMatch.Groups[1].Value;  // e.g., "216"
            if (!text.StartsWith("Chapter ", StringComparison.OrdinalIgnoreCase)) continue;

            string chapterNumber;
            string? chapterTitle;

            // Primary: Extract title from text after "Chapter {chapterId}"
            var chapterStr = $"Chapter {chapterId}";
            var index = text.IndexOf(chapterStr, StringComparison.OrdinalIgnoreCase);
            if (index >= 0)
            {
                var titleStart = index + chapterStr.Length;
                string rawTitle = text.Substring(titleStart).TrimStart();
                // Trim leading punctuation/space (e.g., " - 19th Floor" → "19th Floor")
                rawTitle = rawTitle.TrimStart('-', ':', ' ', '\t');
                chapterTitle = HtmlEntity.DeEntitize(rawTitle);  // Decode entities
                chapterNumber = chapterId;
            }
            else
            {
                // Fallback: Original regex parsing (for non-numeric IDs or mismatches)
                Regex chapterRegex = new(@"Chapter ([0-9]+(?:\.[0-9]+)?)(.*)?");
                Match match = chapterRegex.Match(text);
                if (!match.Success) continue;

                chapterNumber = match.Groups[1].Value;
                string? rawTitle = match.Groups[2].Success ? match.Groups[2].Value.TrimStart('-', ':', ' ', '\t') : null;
                chapterTitle = rawTitle != null ? HtmlEntity.DeEntitize(rawTitle) : null;
            }

            if (!float.TryParse(chapterNumber, out _)) continue;  // Invalid number

            chapterNumber = NormalizeNumber(chapterNumber);

            Chapter chapter = new(manga.Obj, chapterNumber, null, chapterTitle);
            MangaConnectorId<Chapter> mcId = new(chapter, this, href, url);
            chapter.MangaConnectorIds.Add(mcId);
            chapters.Add((chapter, mcId));
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

    private static string NormalizeNumber(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return "0";
        input = input.Trim();
        var match = Regex.Match(input, @"^0*(\d+(?:\.\d+)?)");
        return match.Success ? match.Groups[1].Value : input;
    }

    private static bool IsValidImageUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;
        string lowerUrl = url.ToLowerInvariant();
        return lowerUrl.Contains(".jpg") || lowerUrl.Contains(".jpeg") || lowerUrl.Contains(".png") || lowerUrl.Contains(".webp");
    }
}