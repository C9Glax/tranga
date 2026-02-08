using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using log4net;
using System.Collections.Generic;
using System.Linq;               // For OrderBy, DistinctBy …
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;

namespace API.MangaConnectors;

/// <summary>
/// Connector for https://comix.to/
/// </summary>
public class ComixConnector : MangaConnector
{
    public ComixConnector()
        : base(
            "Comix",                                 // connector name
            new[] { "en" },                          // supported languages (adjust if you add more)
            new[] { "comix.to" },                    // host names that belong to this site
            "https://comix.to/static/favicon.ico")  // placeholder icon – replace with a real one if you have it
    {
        // The original WeebCentral connector used HttpDownloadClient for everything.
        // comix.to also works fine with the same client, but we keep ChromiumDownloadClient
        // for chapter‑image fetching (it needs the referrer header).
        this.downloadClient = new HttpDownloadClient();
    }

    #region SEARCH -------------------------------------------------------------

    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        Log.InfoFormat("Searching on comix.to: {0}", mangaSearchName);

        // 1️⃣ Normalise the query – keep only alphanumerics and spaces.
        string sanitizedTitle = string.Join(' ',
            Regex.Matches(mangaSearchName, @"[A-Za-z0-9]+")
                 .Select(m => m.Value)
                 .Where(v => v.Length > 0))
            .ToLowerInvariant();

        // 2️⃣ Build the search URL.  comix.to uses a simple GET param called "keyword".
        string searchUrl = $"https://comix.to/search?keyword={HttpUtility.UrlEncode(sanitizedTitle)}";

        HttpResponseMessage response = downloadClient
            .MakeRequest(searchUrl, RequestType.Default)
            .GetAwaiter()
            .GetResult();

        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Search request failed – status: " + (int)response.StatusCode);
            return [];
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        // 3️⃣ Each result is an <a> that points to /title/<slug>/ .
        HtmlNodeCollection? nodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/title/')]");
        Log.DebugFormat("Found {0} title links in search results", nodes?.Count ?? 0);
        if (nodes == null || nodes.Count == 0)
            return [];

        var seenUrls = new HashSet<string>();
        var mangas   = new List<(Manga, MangaConnectorId<Manga>)>();

        foreach (HtmlNode node in nodes)
        {
            string href = node.GetAttributeValue("href", "").Trim();
            if (string.IsNullOrEmpty(href))
                continue;

            // Ensure we have an absolute URL – comix.to returns relative links.
            string fullUrl = href.StartsWith("http")
                ? href
                : $"https://comix.to{(href.StartsWith("/") ? "" : "/")}{href}";

            if (!seenUrls.Add(fullUrl))
                continue; // duplicate result

            Log.DebugFormat("Parsing manga from {0}", fullUrl);
            var parsed = GetMangaFromUrl(fullUrl);
            if (parsed.HasValue)
            {
                mangas.Add(parsed.Value);
                Log.DebugFormat("Added '{0}'", parsed.Value.Item1.Name);
            }
            else
            {
                Log.WarnFormat("Failed to parse manga at {0}", fullUrl);
            }
        }

        // De‑duplicate by the unique manga key (generated from its title).
        return mangas.DistinctBy(r => r.Item1.Key).ToArray();
    }

    #endregion

    #region MANGA INFO ---------------------------------------------------------

    /// <summary>
    /// Parses a manga page given any URL that contains "/title/<slug>/".  
    /// The method extracts the slug, fetches the page once and forwards to the shared parser.
    /// </summary>
    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
        Log.InfoFormat("Fetching manga info from: {0}", url);

        // Example URL:
        //   https://comix.to/title/5zrxl-kanojo-no-carrera/
        //   https://comix.to/title/5zrxl-kanojo-no-carrera
        var match = Regex.Match(url,
            @"https?://(?:www\.)?comix\.to/title/(?<slug>[^/]+)/?",
            RegexOptions.IgnoreCase);

        if (!match.Success)
        {
            Log.Error("URL does not match comix.to title pattern");
            return null;
        }

        string slug = match.Groups["slug"].Value;               // e.g. 5zrxl-kanojo-no-carrera
        string canonicalUrl = $"https://comix.to/title/{slug}/";

        HttpResponseMessage response = downloadClient
            .MakeRequest(canonicalUrl, RequestType.MangaInfo)
            .GetAwaiter()
            .GetResult();

        if (!response.IsSuccessStatusCode)
        {
            Log.Error($"Failed to retrieve manga page – status {(int)response.StatusCode}");
            return null;
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var doc    = new HtmlDocument();
        doc.LoadHtml(html);

        // All the heavy lifting is done in a helper so we can reuse it from GetMangaFromId.
        return ParseMangaFromHtml(doc, slug, canonicalUrl);
    }

    /// <summary>
    /// Direct fetch when we already know the slug (e.g. from DB).
    /// </summary>
    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
        // The ID on comix.to is exactly the slug that appears in the URL.
        string url = $"https://comix.to/title/{mangaIdOnSite}/";

        HttpResponseMessage response = downloadClient
            .MakeRequest(url, RequestType.MangaInfo)
            .GetAwaiter()
            .GetResult();

        if (!response.IsSuccessStatusCode)
            return null;

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var doc    = new HtmlDocument();
        doc.LoadHtml(html);

        return ParseMangaFromHtml(doc, mangaIdOnSite, url);
    }

    /// <summary>
    /// Shared HTML parser – extracts title, cover, description, tags, status, authors and year.
    /// The XPath queries were written against the current comix.to markup (April 2024).  
    /// If the site changes you only need to adjust these selectors.
    /// </summary>
    private (Manga, MangaConnectorId<Manga>) ParseMangaFromHtml(
        HtmlDocument doc,
        string mangaSlugOnSite,
        string url)
    {
        // ----- TITLE --------------------------------------------------------
        // <title>5zrxl – Kanojo no Carrera | comix.to</title>
        var titleNode = doc.DocumentNode.SelectSingleNode("//title");
        string rawTitle = titleNode?.InnerText ?? mangaSlugOnSite;
        var titleMatch = Regex.Match(rawTitle, @"^(.*?)\s*\|\s*comix\.to", RegexOptions.IgnoreCase);
        string cleanTitle = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : rawTitle;
        cleanTitle = HtmlEntity.DeEntitize(cleanTitle);

        // ----- COVER ---------------------------------------------------------
        // <img class="cover" src="/media/covers/xxxx.jpg" alt="Cover">
        var coverNode = doc.DocumentNode.SelectSingleNode("//img[contains(@class,'cover')]");
        string coverUrl = coverNode?.GetAttributeValue("src", "") ?? "";
        if (!string.IsNullOrEmpty(coverUrl) && !coverUrl.StartsWith("http"))
            coverUrl = $"https://comix.to{coverUrl}";

        // ----- DESCRIPTION ---------------------------------------------------
        // <div class="description"><p>…</p></div>
        var descNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'description')]");
        string description = HtmlEntity.DeEntitize(descNode?.InnerText ?? "").Trim();

        // ----- TAGS (Genres) -------------------------------------------------
        // <a href="/genre/xyz" class="tag">Action</a>
        var tagNodes = doc.DocumentNode.SelectNodes("//a[contains(@class,'tag')]");
        List<MangaTag> tags = tagNodes?
            .Select(n => new MangaTag(HtmlEntity.DeEntitize(n.InnerText.Trim())))
            .ToList() ?? new List<MangaTag>();

        // ----- STATUS --------------------------------------------------------
        // <span class="status">Ongoing</span>
        var statusNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class,'status')]");
        string rawStatus = HtmlEntity.DeEntitize(statusNode?.InnerText ?? "").ToLowerInvariant().Trim();
        MangaReleaseStatus releaseStatus = rawStatus switch
        {
            "ongoing"   => MangaReleaseStatus.Continuing,
            "hiatus"    => MangaReleaseStatus.OnHiatus,
            "completed" => MangaReleaseStatus.Completed,
            "canceled" or "cancelled" => MangaReleaseStatus.Cancelled,
            _           => MangaReleaseStatus.Unreleased
        };

        // ----- AUTHORS --------------------------------------------------------
        // <a href="/author/xyz" class="author">John Doe</a>
        var authorNodes = doc.DocumentNode.SelectNodes("//a[contains(@class,'author')]");
        List<Author> authors = authorNodes?
            .Select(n => new Author(HtmlEntity.DeEntitize(n.InnerText.Trim())))
            .ToList() ?? new List<Author>();

        // ----- YEAR (first release) -----------------------------------------
        // <span class="year">2021</span>
        var yearNode = doc.DocumentNode.SelectSingleNode("//span[contains(@class,'year')]");
        uint? year = null;
        if (uint.TryParse(yearNode?.InnerText.Trim(), out uint parsedYear))
            year = parsedYear;

        // ----- BUILD OBJECTS -------------------------------------------------
        List<AltTitle> altTitles = new();   // comix.to does not expose alternate titles at the moment
        List<Link> links       = new();

        var manga = new Manga(
            cleanTitle,
            description,
            coverUrl,
            releaseStatus,
            authors,
            tags,
            links,
            altTitles,
            null,          // language – keep null so the key is deterministic
            0f,            // rating (unknown)
            year,
            null);         // extra data

        var mcId = new MangaConnectorId<Manga>(manga, this, mangaSlugOnSite, url);
        manga.MangaConnectorIds.Add(mcId);

        return (manga, mcId);
    }

    #endregion

    #region CHAPTER LIST -------------------------------------------------------

    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(
        MangaConnectorId<Manga> manga,
        string? language = null)
    {
        Log.InfoFormat("Fetching chapter list for slug: {0}", manga.IdOnConnectorSite);

        // The stored ID is the slug we extracted earlier.
        string slug = manga.IdOnConnectorSite;

        // Full‑chapter page – same layout as WeebCentral but under a different path.
        string chaptersUrl = $"https://comix.to/title/{slug}/full-chapter-list";

        HttpResponseMessage response = downloadClient
            .MakeRequest(chaptersUrl, RequestType.Default)
            .GetAwaiter()
            .GetResult();

        if (!response.IsSuccessStatusCode)
        {
            Log.Error($"Failed to load chapter list – status {(int)response.StatusCode}");
            return [];
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        var doc    = new HtmlDocument();
        doc.LoadHtml(html);

        // Each chapter appears as a link that contains "/title/<slug>/<numeric-id>-chapter-<num>"
        var chapterNodes = doc.DocumentNode.SelectNodes("//a[contains(@href, '/title/') and contains(@href, '-chapter-')]");
        if (chapterNodes == null || chapterNodes.Count == 0)
            return [];

        var chapters = new List<(Chapter, MangaConnectorId<Chapter>)>();

        foreach (var node in chapterNodes)
        {
            string href = node.GetAttributeValue("href", "").Trim();
            if (string.IsNullOrEmpty(href))
                continue;

            // Ensure we have an absolute URL for later requests.
            string fullUrl = href.StartsWith("http")
                ? href
                : $"https://comix.to{(href.StartsWith("/") ? "" : "/")}{href}";

            // The visible text often looks like:
            //   "Chapter 1", "Vol.2 Chapter 5 – Special", etc.
            string nodeText = node.InnerText.Trim();

            // ---- VOLUME (optional) -----------------------------------------
            int? volumeNumber = null;
            var volMatch = Regex.Match(nodeText, @"(?:vol\.?|volume|season)\s*([0-9]+)", RegexOptions.IgnoreCase);
            if (volMatch.Success && int.TryParse(volMatch.Groups[1].Value, out int v))
                volumeNumber = v;

            // ---- CHAPTER NUMBER ---------------------------------------------
            string chapterNumber;
            var chMatch = Regex.Match(nodeText, @"(?:ch\.?|chapter)\s*([0-9]+(?:\.[0-9]+)?)", RegexOptions.IgnoreCase);
            if (chMatch.Success)
                chapterNumber = chMatch.Groups[1].Value;
            else
            {
                // Fallback – take the last numeric token we can find.
                var numbers = Regex.Matches(nodeText, @"[0-9]+(?:\.[0-9]+)?")
                                   .Cast<Match>()
                                   .Select(m => m.Value)
                                   .ToArray();

                if (numbers.Length == 0)
                {
                    Log.Warn($"Unable to determine chapter number from '{nodeText}'. Skipping.");
                    continue;
                }

                chapterNumber = numbers.Last();
            }

            // ---- BUILD CHAPTER OBJECT ---------------------------------------
            var chapter = new Chapter(manga.Obj, chapterNumber, volumeNumber, null);

            // The ID we store is the numeric part that appears *before* "-chapter-".
            // Example: 2407319-chapter-1 => "2407319"
            string idOnSite = new Uri(fullUrl).Segments.Last()
                                .Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries)[0];

            var mcId = new MangaConnectorId<Chapter>(chapter, this, idOnSite, fullUrl);
            chapter.MangaConnectorIds.Add(mcId);

            chapters.Add((chapter, mcId));
        }

        Log.InfoFormat("Found {0} chapters for '{1}'", chapters.Count, manga.Obj.Name);
        // Sort using the same comparer that MangaCentral uses (numeric + volume aware)
        return chapters.OrderBy(c => c.Item1, new Chapter.ChapterComparer()).ToArray();
    }

    #endregion

    #region CHAPTER IMAGES -----------------------------------------------------

    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        Log.InfoFormat("Fetching image URLs for chapter: {0}", chapterId.Obj);

        if (chapterId.WebsiteUrl == null)
        {
            Log.Error("Chapter URL is null – cannot continue.");
            return [];
        }

        // comix.to checks the referrer header.  We pass the manga page as referrer
        // because that’s what the site expects when you click “Read”.
        string? referrer = null;
        if (chapterId.Obj.ParentManga.MangaConnectorIds?.Any() == true)
        {
            referrer = chapterId.Obj.ParentManga.MangaConnectorIds
                .FirstOrDefault(id => id.MangaConnectorName == this.Name)?
                .WebsiteUrl;
        }

        return GetChapterImageUrlsAsync(chapterId, referrer).GetAwaiter().GetResult();
    }

    private async Task<string[]> GetChapterImageUrlsAsync(
        MangaConnectorId<Chapter> chapterId,
        string? referrer)
    {
        await using var chromium = new ChromiumDownloadClient();

        HttpResponseMessage response = await chromium.MakeRequest(
            chapterId.WebsiteUrl!,
            RequestType.Default,
            referrer);

        if (!response.IsSuccessStatusCode)
        {
            Log.Error($"Failed to load chapter page – status {(int)response.StatusCode}");
            return [];
        }

        string html = await response.Content.ReadAsStringAsync();
        var doc    = new HtmlDocument();
        doc.LoadHtml(html);

        // Images are like: <img alt="Page 1" src="/media/manga/xxxxx.jpg">
        var imgNodes = doc.DocumentNode.SelectNodes("//img[starts-with(@alt, 'Page')]");
        if (imgNodes == null || imgNodes.Count == 0)
        {
            Log.Warn("No page images found on chapter page.");
            return [];
        }

        var urls = imgNodes
            .Select(img =>
            {
                string src = img.GetAttributeValue("src", "")
                             ?? img.GetAttributeValue("data-src", "");

                // Make absolute if necessary.
                if (!string.IsNullOrEmpty(src) && !src.StartsWith("http"))
                    src = $"https://comix.to{src}";
                return src;
            })
            .Where(u => !string.IsNullOrEmpty(u))
            .ToArray();

        Log.InfoFormat("Found {0} image URLs for chapter {1}", urls.Length, chapterId.Obj);
        return urls;
    }

    #endregion
}