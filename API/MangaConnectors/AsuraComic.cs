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
    public AsuraComic() : base("AsuraComic", ["en"], ["asuracomic.net"], "https://asuracomic.net/images/logo.webp")
    {
        this.downloadClient = new HttpDownloadClient(); // Use Http for all
    }

    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        Log.InfoFormat("Searching: {0}", mangaSearchName);
        string sanitizedTitle = string.Join(' ', Regex.Matches(mangaSearchName, @"[A-Za-z]+").Where(m => m.Value.Length > 0)).ToLowerInvariant();
        string requestUrl = $"https://asuracomic.net/series?name={HttpUtility.UrlEncode(sanitizedTitle)}";
        HttpResponseMessage response = downloadClient.MakeRequest(requestUrl, RequestType.Default).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Request failed or no HTML retrieved");
            return [];
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        Log.DebugFormat("Search HTML length: {0}", html.Length);
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        HtmlNodeCollection? nodes = doc.DocumentNode.SelectNodes("//a[starts-with(@href, 'series/')]"); // Match v1 XPath
        Log.DebugFormat("Found {0} series nodes in search HTML", nodes?.Count ?? 0);
        if (nodes is null || nodes.Count < 1)
        {
            Log.Error("No series links found");
            return [];
        }

        HashSet<string> seenUrls = new(); // Dedup URLs
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
                    Log.DebugFormat("Fetching from {0}", fullUrl); // Debug URL
                    (Manga, MangaConnectorId<Manga>)? manga = GetMangaFromUrl(fullUrl);
                    if (manga.HasValue)
                    {
                        mangas.Add(manga.Value);
                        Log.DebugFormat("Added manga from {0}", fullUrl);
                    }
                    else
                    {
                        Log.WarnFormat("Failed to parse manga from {0}", fullUrl); // Debug fails
                    }
                }
            }
        }

        Log.InfoFormat("Search '{0}' yielded {1} results.", mangaSearchName, mangas.Count);
        return mangas.DistinctBy(r => r.Item1.Key).ToArray(); // Dedup by manga Key
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
        Log.InfoFormat("Fetching manga from URL: {0}", url);
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
        HtmlDocument doc = new();
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
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        return ParseMangaFromHtml(doc, mangaIdOnSite, url); // Use full slug as ID
    }

    private (Manga, MangaConnectorId<Manga>) ParseMangaFromHtml(HtmlDocument doc, string mangaIdOnSite, string url)
    {
        // Title with cleanup (kept for robustness, but simple decode to match original)
        HtmlNode? titleNode = doc.DocumentNode.SelectSingleNode("//title");
        string rawTitle = Regex.Match(titleNode?.InnerText ?? mangaIdOnSite, @"(.*) - Asura Scans").Groups[1].Value.Trim();
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
        
        MangaConnectorId<Manga> mcId = new(manga, this, mangaIdOnSite, url);
        manga.MangaConnectorIds.Add(mcId);
        
        return (manga, mcId);
    }

    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> manga, string? language = null)
    {
        Log.InfoFormat("Fetching chapters for: {0}", manga.IdOnConnectorSite);
        // Strip unique suffix for base slug (if any, but none now)
        string baseSlug = manga.IdOnConnectorSite.Contains('-') ? manga.IdOnConnectorSite[..manga.IdOnConnectorSite.LastIndexOf('-')] : manga.IdOnConnectorSite;
        string websiteUrl = manga.WebsiteUrl ?? $"https://asuracomic.net/series/{baseSlug}";
        HttpResponseMessage response = downloadClient.MakeRequest(websiteUrl, RequestType.Default).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to load chapters page");
            return [];
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        HtmlNodeCollection? chapterNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/chapter/')]");
        if (chapterNodes is null)
            return [];

        HashSet<string> seenHrefs = new();
        List<(Chapter, MangaConnectorId<Chapter>)> chapters = new();
        foreach (HtmlNode chapterNode in chapterNodes)
        {
            string href = chapterNode.GetAttributeValue("href", "");
            if (string.IsNullOrEmpty(href) || !seenHrefs.Add(href))
                continue;

            // Normalize href (ensure leading /)
            if (!href.StartsWith("/"))
                href = "/" + href;

            // Extract /chapter/N part and prepend /series/{baseSlug} (avoids duplication)
            int chapterIndex = href.IndexOf("/chapter/", StringComparison.OrdinalIgnoreCase);
            string fullUrl;
            if (chapterIndex > 0)
            {
                string chapterPath = href.Substring(chapterIndex);
                fullUrl = $"https://asuracomic.net/series/{baseSlug}{chapterPath}";
            }
            else
            {
                fullUrl = $"https://asuracomic.net/series/{baseSlug}{href}";
            }

            string text = chapterNode.InnerText.Trim();
            string chapterNumber;
            string? chapterTitle;

            // Primary: Extract title from text after "Chapter {chapterId}" (from href)
            Match chIdMatch = Regex.Match(href, @"/chapter/(\d+)");
            if (chIdMatch.Success)
            {
                string hrefChId = chIdMatch.Groups[1].Value;
                string chapterStr = $"Chapter {hrefChId}";
                int index = text.IndexOf(chapterStr, StringComparison.OrdinalIgnoreCase);
                if (index >= 0)
                {
                    int titleStart = index + chapterStr.Length;
                    string rawTitle = text.Substring(titleStart).TrimStart();
                    rawTitle = rawTitle.TrimStart('-', ':', ' ', '\t');
                    chapterTitle = HtmlEntity.DeEntitize(rawTitle);
                    chapterNumber = hrefChId;
                }
                else
                {
                    // Fallback: Original regex parsing
                    Regex chapterRegex = new(@"Chapter ([0-9]+(?:\.[0-9]+)?)(.*)?");
                    Match match = chapterRegex.Match(text);
                    if (!match.Success) continue;

                    chapterNumber = match.Groups[1].Value;
                    string? rawTitle = match.Groups[2].Success ? match.Groups[2].Value.TrimStart('-', ':', ' ', '\t') : null;
                    chapterTitle = rawTitle != null ? HtmlEntity.DeEntitize(rawTitle) : null;
                }
            }
            else
            {
                continue;  // Skip if no chapter ID in href
            }

            Chapter ch = new(manga.Obj, chapterNumber, null, chapterTitle);
            string chapterIdFromHref = href.Substring(href.LastIndexOf('/') + 1);
            // Fix: Use manga's hash + chapter number for unique ID (consistent on re-run)
            string hash = baseSlug.Split('-').Last();  // Extract hash (e.g., "fc9f12dc")
            string uniqueChapterId = $"{hash}-{chapterIdFromHref}";
            MangaConnectorId<Chapter> mcId = new(ch, this, uniqueChapterId, fullUrl);
            ch.MangaConnectorIds.Add(mcId);
            chapters.Add((ch, mcId));
        }

        Log.InfoFormat("Found {0} chapters for {1}", chapters.Count, manga.Obj.Name);
        return chapters.OrderBy(c => c.Item1, new Chapter.ChapterComparer()).ToArray();
    }

    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        Log.InfoFormat("Getting Chapter Image-Urls: {0}", chapterId.Obj);
        if (chapterId.WebsiteUrl is null)
        {
            Log.Error("Chapter URL is null");
            return [];
        }

        // Get manga's series URL as referrer (simulates navigation from chapter list)
        string? referrer = null;
        if (chapterId.Obj.ParentManga.MangaConnectorIds is not null && chapterId.Obj.ParentManga.MangaConnectorIds.Any())
        {
            referrer = chapterId.Obj.ParentManga.MangaConnectorIds
                .FirstOrDefault(id => id.MangaConnectorName == this.Name)?.WebsiteUrl;
        }

        // Use Chromium for JS-rendered images; dispose after use
        using ChromiumDownloadClient chromium = new ChromiumDownloadClient();
        HttpResponseMessage response = chromium.MakeRequest(chapterId.WebsiteUrl!, RequestType.Default, referrer).GetAwaiter().GetResult();

        if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
        {
            Log.Error("Failed to load chapter page with Chromium");
            return [];
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        // Temporary debug: Log HTML snippet (remove after verification)
        Log.DebugFormat("HTML snippet (first 500 chars): {0}", doc.DocumentNode.OuterHtml.Substring(0, Math.Min(500, doc.DocumentNode.OuterHtml.Length)));

        // v1 selector: //img[contains(@alt, 'chapter page')]
        HtmlNodeCollection? imageNodes = doc.DocumentNode.SelectNodes("//img[contains(@alt, 'chapter page') or contains(@alt, 'Chapter') or @class='page-break']");
        if (imageNodes is null || imageNodes.Count == 0)
        {
            Log.Warn("No chapter page images found");
            return [];
        }

        string[] imageUrls = imageNodes
            .Select(i => 
            {
                string src = i.GetAttributeValue("src", "");
                if (string.IsNullOrEmpty(src))
                    src = i.GetAttributeValue("data-src", ""); // Fallback for lazy-load
                if (!src.StartsWith("http"))
                    src = "https://asuracomic.net" + src; // Absolute URL if relative
                return src;
            })
            .Where(u => !string.IsNullOrEmpty(u))
            .ToArray();

        Log.InfoFormat("Found {0} images for chapter {1}", imageUrls.Length, chapterId.Obj);
        return imageUrls;
    }
}