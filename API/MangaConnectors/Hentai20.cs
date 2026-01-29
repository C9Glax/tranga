using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using log4net;
using System.Collections.Generic;
using System.Linq;

namespace API.MangaConnectors;

public class Hentai20 : MangaConnector
{
    public Hentai20()
        : base(
            "Hentai20",
            ["en"],
            ["hentai20.io"],
            "https://hentai20.io/wp-content/uploads/2024/05/cropped-210da20ddb1be20edd43583bcaf1061f628cbc16-300x300.jpg"
        )
    {
        this.downloadClient = new HttpDownloadClient();
    }

    // =========================
    // SEARCH
    // =========================
    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        Log.InfoFormat("Searching Hentai20.io for: {0}", mangaSearchName);
		string sanitizedTitle = string.Join(' ', Regex.Matches(mangaSearchName, @"[A-Za-z]+").Where(m => m.Value.Length > 0)).ToLowerInvariant();
        string requestUrl = $"https://hentai20.io/?s={HttpUtility.UrlEncode(sanitizedTitle)}";
        HttpResponseMessage response = downloadClient.MakeRequest(requestUrl, RequestType.Default).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Search request failed");
            return [];
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        HtmlNodeCollection? nodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'bsx')]//a[@href]");
        Log.DebugFormat("Found {0} series nodes in search HTML", nodes?.Count ?? 0);
        if (nodes is null || nodes.Count == 0)
        {
            Log.Warn("No search results found");
            return [];
        }

        HashSet<string> seenUrls = new();
        List<(Manga, MangaConnectorId<Manga>)> mangas = new();

        foreach (HtmlNode node in nodes)
        {
            string href = node.GetAttributeValue("href", "").Trim();
            if (string.IsNullOrEmpty(href) || !seenUrls.Add(href))
                continue;
			
			Log.DebugFormat("Fetching from {0}", href);

            (Manga, MangaConnectorId<Manga>)? manga = GetMangaFromUrl(href);
            if (manga.HasValue)
			{
                mangas.Add(manga.Value);
				Log.DebugFormat("Added manga from {0}", href);
			}
			else
			{
				Log.WarnFormat("Failed to parse manga from {0}", href);
			}
        }

        Log.InfoFormat("Search '{0}' yielded {1} results.", mangaSearchName, mangas.Count);
        return mangas.DistinctBy(r => r.Item1.Key).ToArray();
    }

    // =========================
    // MANGA PAGE
    // =========================
    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
		Log.InfoFormat("Fetching manga from URL: {0}", url);
		
        Match urlMatch = Regex.Match(url, @"https?://hentai20\.io/manga/(?<slug>[^/]+)/?");
        if (!urlMatch.Success)
            return null;

        string slug = urlMatch.Groups["slug"].Value;

        HttpResponseMessage response =
            downloadClient.MakeRequest(url, RequestType.MangaInfo)
                          .GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to retrieve manga page");
            return null;
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        return ParseMangaFromHtml(doc, slug, url);
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
		string url = $"https://hentai20.io/manga/{mangaIdOnSite}";
        HttpResponseMessage response = downloadClient.MakeRequest(url, RequestType.MangaInfo).GetAwaiter().GetResult();
        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to retrieve manga page");
            return null;
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        return ParseMangaFromHtml(doc, mangaIdOnSite, url);
    }

    private (Manga, MangaConnectorId<Manga>) ParseMangaFromHtml(HtmlDocument doc, string mangaIdOnSite, string url)
    {
		// Title
        HtmlNode? titleNode = doc.DocumentNode.SelectSingleNode("//h1");
        string title = HtmlEntity.DeEntitize(titleNode?.InnerText ?? mangaIdOnSite).Trim();

		// Cover
        HtmlNode? coverNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'thumb')]//img");
        string coverUrl = coverNode?.GetAttributeValue("src", "") ?? "";

        // Description
		HtmlNode? descNode = doc.DocumentNode.SelectSingleNode("//div[contains(@class,'entry-content')]/p");
        string description = HtmlEntity.DeEntitize(descNode?.InnerText ?? "").Trim();

		// Tags
        HtmlNodeCollection? tagNodes = doc.DocumentNode.SelectNodes("//div[contains(@class,'seriestugenre')]//a");
		List<MangaTag> tags = tagNodes?.Select(t => new MangaTag(HtmlEntity.DeEntitize(t.InnerText.Trim()))).ToList() ?? [];
		
		// Status
        HtmlNode? statusNode = doc.DocumentNode.SelectSingleNode("//tr[td[1][normalize-space()='Status']]/td[2]");
        string rawStatus = HtmlEntity.DeEntitize(statusNode?.InnerText ?? "").ToLowerInvariant().Trim();
        MangaReleaseStatus releaseStatus = rawStatus switch
        {
            "ongoing" => MangaReleaseStatus.Continuing,
            "hiatus" => MangaReleaseStatus.OnHiatus,
            "completed" => MangaReleaseStatus.Completed,
            "canceled" => MangaReleaseStatus.Cancelled,
            _ => MangaReleaseStatus.Unreleased
        };

        // Match original constructor (null language for consistent Key)
		Manga manga = new(title, description, coverUrl, releaseStatus, [], tags, [], [], null, 0f, null, null);

        // Use mangaIdOnSite for ID (core slug, consistent)
		MangaConnectorId<Manga> mcId = new(manga, this, mangaIdOnSite, url);
        manga.MangaConnectorIds.Add(mcId);
		
        return (manga, mcId);
    }

    // =========================
    // CHAPTERS
    // =========================
    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> manga, string? language = null)
    {
		Log.InfoFormat("Fetching chapters for: {0}", manga.Obj.Name);
		
        HttpResponseMessage response = downloadClient.MakeRequest(manga.WebsiteUrl!, RequestType.Default).GetAwaiter().GetResult();

        if (!response.IsSuccessStatusCode)
        {
            Log.Error("Failed to load chapters page");
            return [];
        }

        string html = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();
        HtmlDocument doc = new();
        doc.LoadHtml(html);

        // Extract chapters from page
		HtmlNodeCollection? chapterNodes = doc.DocumentNode.SelectNodes("//ul[contains(@class,'clstyle')]//a");

        if (chapterNodes is null)
            return [];

        List<(Chapter, MangaConnectorId<Chapter>)> chapters = new();
        HashSet<string> seen = new();

        foreach (HtmlNode node in chapterNodes)
        {
            string href = node.GetAttributeValue("href", "").Trim();
            if (string.IsNullOrEmpty(href) || !seen.Add(href))
                continue;

            // Get chapter number - supports decimals
			string chapterNumber;
			Match chMatch = Regex.Match(node.InnerText, @"Chapter\s*([\d\.]+)", RegexOptions.IgnoreCase);
            if (chMatch.Success)
				chapterNumber = chMatch.Groups[1].Value;
			else
			{
				Log.Warn($"Failed to parse chapter number: {chMatch.Groups[1].Value}");
				continue;
			}
			
			string? title = null;

            Chapter ch = new(manga.Obj, chapterNumber, null, title);
            string chapterIdOnSite = $"{manga.IdOnConnectorSite}-{chapterNumber.Replace(".", "_")}";

            MangaConnectorId<Chapter> mcId = new(ch, this, chapterIdOnSite, href);
            ch.MangaConnectorIds.Add(mcId);
            chapters.Add((ch, mcId));
        }
		
        Log.InfoFormat("Found {0} chapters for {1}", chapters.Count, manga.Obj.Name);
        return chapters.OrderBy(c => c.Item1, new Chapter.ChapterComparer()).ToArray();
    }

    // =========================
    // IMAGES
    // =========================
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
		
		return GetChapterImageUrlsAsync(chapterId, referrer).GetAwaiter().GetResult();
	}
	
	private async Task<string[]> GetChapterImageUrlsAsync(MangaConnectorId<Chapter> chapterId, string? referrer)
	{
		await using ChromiumDownloadClient chromium = new ChromiumDownloadClient();
		
        HttpResponseMessage response = await chromium.MakeRequest(chapterId.WebsiteUrl!, RequestType.Default, referrer);

        if ((int)response.StatusCode < 200 || (int)response.StatusCode >= 300)
		{
			Log.Error("Failed to load chapter page with Chromium");
			return [];
		}

        string html = await response.Content.ReadAsStringAsync();
		
		HtmlDocument doc = new();
		doc.LoadHtml(html);
		
        HtmlNodeCollection? imageNodes = doc.DocumentNode.SelectNodes("//img[contains(@class,'ts-main-image')]");

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
				return src;
			})
			.Where(u => !string.IsNullOrEmpty(u))
			.ToArray();
			
		Log.InfoFormat("Found {0} images for chapter {1}", imageUrls.Length, chapterId.Obj);
		return imageUrls;
    }
}
