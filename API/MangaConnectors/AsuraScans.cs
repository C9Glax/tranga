using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using log4net;
using System.Collections.Generic;
using System.Linq; // For OrderBy
using System.Text.Json;
using System.Text;
using System.Threading;

namespace API.MangaConnectors;

public class AsuraScans : MangaConnector
{
    public AsuraScans() : base("AsuraScans", ["en"], ["asurascans.com"], "https://asurascans.com/images/logo.webp")
    {
        this.downloadClient = new HttpDownloadClient();
    }

    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        Log.InfoFormat("Searching: {0}", mangaSearchName);
        string sanitizedTitle = string.Join(' ', Regex.Matches(mangaSearchName, @"[A-Za-z]+").Where(m => m.Value.Length > 0)).ToLowerInvariant();
        // Browse page requires JS for filtering — use Chromium so the filter executes
        string requestUrl = $"https://asurascans.com/browse?search={HttpUtility.UrlEncode(sanitizedTitle)}";

        string html;
        ChromiumDownloadClient chromium = new();
        try
        {
            HttpResponseMessage response = chromium.MakeRequest(requestUrl, RequestType.Default).GetAwaiter().GetResult();
            if (!response.IsSuccessStatusCode)
            {
                Log.Error("Request failed or no HTML retrieved");
                return [];
            }
            html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        }
        finally
        {
            chromium.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        Log.DebugFormat("Search HTML length: {0}", html.Length);
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        // Hrefs are /comics/slug — note the leading slash; exclude chapter links
        HtmlNodeCollection? nodes = doc.DocumentNode.SelectNodes("//a[starts-with(@href, '/comics/') and not(contains(@href, '/chapter/'))]");
        Log.DebugFormat("Found {0} comics nodes in search HTML", nodes?.Count ?? 0);
        if (nodes is null || nodes.Count < 1)
        {
            Log.Error("No comics links found");
            return [];
        }

        HashSet<string> seenUrls = new();
        List<(Manga, MangaConnectorId<Manga>)> mangas = new();
        foreach (HtmlNode node in nodes)
        {
            string href = node.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(href))
            {
                string fullUrl = href.StartsWith("http") ? href : $"https://asurascans.com{href}";
                if (seenUrls.Add(fullUrl))
                {
                    Log.DebugFormat("Fetching from {0}", fullUrl);
                    (Manga, MangaConnectorId<Manga>)? manga = GetMangaFromUrl(fullUrl);
                    if (manga.HasValue)
                    {
                        mangas.Add(manga.Value);
                        Log.DebugFormat("Added manga from {0}", fullUrl);
                    }
                    else
                    {
                        Log.WarnFormat("Failed to parse manga from {0}", fullUrl);
                    }
                }
            }
        }

        Log.InfoFormat("Search '{0}' yielded {1} results.", mangaSearchName, mangas.Count);
        return mangas.DistinctBy(r => r.Item1.Key).ToArray();
    }

   public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
        Log.InfoFormat("Fetching manga from URL: {0}", url);
        // URL format: https://asurascans.com/comics/slug-uid  (uid = 8 hex chars)
        Match urlMatch = Regex.Match(url, @"https?://(?:www\.)?asurascans\.com/comics/(?<coreSlug>[^-]+(?:-[^-]+)*?)(?:-(?<uid>[a-f0-9]{8}))?$");
        if (!urlMatch.Success)
            return null;

        string coreSlug = urlMatch.Groups["coreSlug"].Value;

        HttpResponseMessage response = downloadClient.MakeRequest(url, RequestType.MangaInfo).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to retrieve manga page");
            return null;
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        // Store the actual URL so GetChapters can fetch it directly
        return ParseMangaFromHtml(doc, coreSlug, url);
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
        // mangaIdOnSite is the core slug; strip any legacy wildcard before building URL
        string cleanId = mangaIdOnSite.TrimEnd('*').TrimEnd('-');
        string url = $"https://asurascans.com/comics/{cleanId}";
        HttpResponseMessage response = downloadClient.MakeRequest(url, RequestType.MangaInfo).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to retrieve manga page");
            return null;
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        return ParseMangaFromHtml(doc, cleanId, url);
    }

    private (Manga, MangaConnectorId<Manga>) ParseMangaFromHtml(HtmlDocument doc, string mangaIdOnSite, string url)
    {
        // Title: use <h1> — page title format changed from "Title - Asura Scans" to "Title | Asura Scans"
        HtmlNode? h1Node = doc.DocumentNode.SelectSingleNode("//h1");
        string cleanTitle = HtmlEntity.DeEntitize(h1Node?.InnerText ?? mangaIdOnSite).Trim();

        // Cover: CDN image under /covers/
        HtmlNode? coverNode = doc.DocumentNode.SelectSingleNode("//img[contains(@src,'cdn.asurascans.com') and contains(@src,'/covers/')]");
        string coverUrl = coverNode?.GetAttributeValue("src", "") ?? "";

        // Description: <p id="description-text">
        HtmlNode? descNode = doc.DocumentNode.SelectSingleNode("//p[@id='description-text']");
        string description = HtmlEntity.DeEntitize(descNode?.InnerText ?? "").Trim();

        // Tags: <a href="/browse?genres=action">Action</a>
        HtmlNodeCollection? genreNodes = doc.DocumentNode.SelectNodes("//a[starts-with(@href, '/browse?genres=')]");
        List<MangaTag> tags = genreNodes?.Select(a => new MangaTag(HtmlEntity.DeEntitize(a.InnerText.Trim()))).ToList() ?? [];

        // Status: <span>Status</span> <span>ongoing</span>
        HtmlNode? statusNode = doc.DocumentNode.SelectSingleNode("//span[text()='Status']/following-sibling::span[1]");
        string rawStatus = HtmlEntity.DeEntitize(statusNode?.InnerText ?? "").ToLowerInvariant().Trim();
        MangaReleaseStatus releaseStatus = rawStatus switch
        {
            "ongoing" or "season end" => MangaReleaseStatus.Continuing,
            "hiatus" => MangaReleaseStatus.OnHiatus,
            "completed" => MangaReleaseStatus.Completed,
            "dropped" => MangaReleaseStatus.Cancelled,
            _ => MangaReleaseStatus.Unreleased
        };

        // Authors: <a href="/browse?author=Name">
        HtmlNodeCollection? authorNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/browse?author=')]");
        // Artists: <a href="/browse?artist=Name">
        HtmlNodeCollection? artistNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/browse?artist=')]");
        List<Author> authors = authorNodes?.Select(a => new Author(HtmlEntity.DeEntitize(a.InnerText.Trim()))).ToList() ?? [];
        if (artistNodes is not null)
            authors.AddRange(artistNodes.Select(a => new Author(HtmlEntity.DeEntitize(a.InnerText.Trim()))));

        // Year: attempt to extract a 4-digit year near the first chapter link
        uint? year = null;
        HtmlNode? yearNode = doc.DocumentNode.SelectSingleNode("//a[contains(@href, '/chapter/1')]/../following-sibling::*[1]");
        if (yearNode?.InnerText is { } yearText)
        {
            Match yearMatch = Regex.Match(yearText, @"\b(20\d{2})\b");
            if (yearMatch.Success && uint.TryParse(yearMatch.Groups[1].Value, out uint parsedYear))
                year = parsedYear;
        }

        List<AltTitle> altTitles = new();
        List<Link> links = new();
        Manga manga = new(cleanTitle, description, coverUrl, releaseStatus, authors, tags, links, altTitles, null, 0f, year, null);

        MangaConnectorId<Manga> mcId = new(manga, this, mangaIdOnSite, url);
        manga.MangaConnectorIds.Add(mcId);

        return (manga, mcId);
    }

    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> manga, string? language = null)
    {
        Log.InfoFormat("Fetching chapters for: {0}", manga.IdOnConnectorSite);

        // Derive baseSlug from IdOnConnectorSite (stored as core slug, e.g. "world-saving-is-a-skill")
        // Never derive it from WebsiteUrl — the old URL might be a full asuracomic.net path
        string baseSlug = manga.IdOnConnectorSite ?? "";
        if (baseSlug.Contains("comics/"))
            baseSlug = baseSlug.Substring(baseSlug.IndexOf("comics/") + 7);
        else if (baseSlug.Contains("series/"))
            baseSlug = baseSlug.Substring(baseSlug.IndexOf("series/") + 7);
        baseSlug = baseSlug.TrimEnd('*').TrimEnd('-');

        // Determine the URL to fetch chapters from.
        // Prefer a stored asurascans.com URL; migrate old asuracomic.net URLs; fallback to slug.
        string websiteUrl;
        string? storedUrl = manga.WebsiteUrl;
        if (!string.IsNullOrEmpty(storedUrl) && storedUrl.Contains("asurascans.com"))
        {
            websiteUrl = storedUrl.TrimEnd('*').TrimEnd('-');
        }
        else if (!string.IsNullOrEmpty(storedUrl) && storedUrl.Contains("asuracomic.net"))
        {
            // Migrate: asuracomic.net/series/slug[-*] → asurascans.com/comics/slug
            Match m = Regex.Match(storedUrl, @"asuracomic\.net/series/([^/?#]+)");
            if (m.Success)
            {
                baseSlug = m.Groups[1].Value.TrimEnd('*').TrimEnd('-');
            }
            websiteUrl = $"https://asurascans.com/comics/{baseSlug}";
        }
        else
        {
            websiteUrl = $"https://asurascans.com/comics/{baseSlug}";
        }

        HttpResponseMessage response = downloadClient.MakeRequest(websiteUrl, RequestType.Default).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to load chapters page");
            return [];
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        // Early Access detection from JSON
        Dictionary<string, bool> earlyAccessDict = new();
        HtmlNode? scriptNode = doc.DocumentNode.SelectSingleNode("//script[contains(text(), 'is_early_access')]");
        if (scriptNode != null)
        {
            string scriptText = scriptNode.InnerText.Replace("\\", "");
            Match? jsonMatch = Regex.Match(scriptText, @"""chapters"":\s*\[(.*?)\],\s*""comic""", RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                string chaptersJson = "[" + jsonMatch.Groups[1].Value.Trim().Trim(',') + "]";
                try
                {
                    byte[] jsonBytes = Encoding.UTF8.GetBytes(chaptersJson);
                    JsonElement jsonChapters = JsonSerializer.Deserialize<JsonElement>(jsonBytes);
                    if (jsonChapters.ValueKind == JsonValueKind.Array)
                    {
                        foreach (JsonElement chapter in jsonChapters.EnumerateArray())
                        {
                            if (chapter.TryGetProperty("name", out JsonElement nameProp))
                            {
                                string chNum = nameProp.ValueKind == JsonValueKind.Number
                                    ? nameProp.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture)
                                    : nameProp.GetString() ?? "";

                                if (!string.IsNullOrEmpty(chNum) &&
                                    chapter.TryGetProperty("is_early_access", out JsonElement eaProp) &&
                                    (eaProp.ValueKind == JsonValueKind.True ||
                                     (eaProp.ValueKind == JsonValueKind.String && eaProp.GetString()?.ToLower() == "true")))
                                {
                                    earlyAccessDict[chNum] = true;
                                }
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // Silent - JSON issues are non-fatal
                }
            }
        }

        // Extract chapters from page
        HtmlNodeCollection? chapterNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/chapter/')]");
        if (chapterNodes is null)
            return [];

        HashSet<string> seenHrefs = new();
        List<(Chapter, MangaConnectorId<Chapter>)> chapters = new();

        foreach (HtmlNode node in chapterNodes)
        {
            string href = node.GetAttributeValue("href", "").Trim();
            if (string.IsNullOrEmpty(href) || !seenHrefs.Add(href))
                continue;

            string fullUrl;
            if (href.StartsWith("http://") || href.StartsWith("https://"))
            {
                // Absolute URL — extract the /chapter/N part and rebuild with our baseSlug
                if (!href.Contains("/chapter/"))
                    continue;
                fullUrl = $"https://asurascans.com/comics/{baseSlug}{href.Substring(href.IndexOf("/chapter/"))}";
            }
            else
            {
                if (!href.StartsWith("/"))
                    href = "/" + href;
                if (!href.Contains("/chapter/"))
                    continue;
                fullUrl = $"https://asurascans.com/comics/{baseSlug}{href.Substring(href.IndexOf("/chapter/"))}";
            }

            string text = node.InnerText.Trim();

            // Get chapter number — supports decimals
            string chapterNumber;
            Match hrefMatch = Regex.Match(href, @"/chapter/([\d\.]+)");
            if (hrefMatch.Success)
            {
                chapterNumber = hrefMatch.Groups[1].Value;
            }
            else
            {
                Match textMatch = Regex.Match(text, @"Chapter\s*([\d\.]+)", RegexOptions.IgnoreCase);
                if (!textMatch.Success)
                    continue;
                chapterNumber = textMatch.Groups[1].Value;
            }

            if (earlyAccessDict.ContainsKey(chapterNumber))
                continue;

            string? title = null;
            string chapterStr = $"Chapter {chapterNumber}";
            int titleIndex = text.IndexOf(chapterStr, StringComparison.OrdinalIgnoreCase);
            if (titleIndex >= 0)
            {
                string rawTitle = text.Substring(titleIndex + chapterStr.Length).TrimStart('-', ':', ' ', '\t');
                title = string.IsNullOrEmpty(rawTitle) ? null : HtmlEntity.DeEntitize(rawTitle);
            }

            Chapter ch = new(manga.Obj, chapterNumber, null, title);
            string uniqueChapterId = $"{baseSlug}-{chapterNumber.Replace(".", "_")}";
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

        string? referrer = null;
        if (chapterId.Obj.ParentManga.MangaConnectorIds is not null && chapterId.Obj.ParentManga.MangaConnectorIds.Any())
        {
            referrer = chapterId.Obj.ParentManga.MangaConnectorIds
                .FirstOrDefault(id => id.MangaConnectorName == this.Name)?.WebsiteUrl;
        }

        ChromiumDownloadClient chromium = new();
        try
        {
            HttpResponseMessage response = chromium.MakeRequest(chapterId.WebsiteUrl!, RequestType.Default, referrer).GetAwaiter().GetResult();

            if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
            {
                Log.Error("Failed to load chapter page with Chromium");
                return [];
            }

            string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            HtmlDocument doc = new();
            doc.LoadHtml(html);

            Log.DebugFormat("HTML snippet (first 500 chars): {0}", doc.DocumentNode.OuterHtml.Substring(0, Math.Min(500, doc.DocumentNode.OuterHtml.Length)));

            // Chapter images have alt="Page N - Chapter N - Title"
            HtmlNodeCollection? imageNodes = doc.DocumentNode.SelectNodes("//img[contains(@alt, '- Chapter') or @class='page-break']");
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
                        src = i.GetAttributeValue("data-src", "");
                    if (!src.StartsWith("http"))
                        src = "https://asurascans.com" + src;
                    return src;
                })
                .Where(u => !string.IsNullOrEmpty(u))
                .ToArray();

            Log.InfoFormat("Found {0} images for chapter {1}", imageUrls.Length, chapterId.Obj);
            return imageUrls;
        }
        finally
        {
            chromium.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }
    }
}
