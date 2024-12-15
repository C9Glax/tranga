using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public class AsuraToon : MangaConnector
{
	
	public AsuraToon() : base("AsuraToon", ["en"], ["https://asuracomic.net"])
	{
		this.downloadClient = new ChromiumDownloadClient();
	}

	public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] GetManga(string publicationTitle = "")
	{
		string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(m => m.Value.Length > 0)).ToLower();
		string requestUrl = $"https://asuracomic.net/series?name={sanitizedTitle}";
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return [];

		if (requestResult.htmlDocument is null)
		{
			return [];
		}
			
		(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
		return publications;
	}

	public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromId(string publicationId)
	{
		return GetMangaFromUrl($"https://asuracomic.net/series/{publicationId}");
	}

	public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromUrl(string url)
	{
		RequestResult requestResult = downloadClient.MakeRequest(url, RequestType.MangaInfo);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return null;
		if (requestResult.htmlDocument is null)
		{
			return null;
		}
		return ParseSinglePublicationFromHtml(requestResult.htmlDocument, url.Split('/')[^1], url);
	}

	private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] ParsePublicationsFromHtml(HtmlDocument document)
	{
		HtmlNodeCollection mangaList = document.DocumentNode.SelectNodes("//a[starts-with(@href,'series')]");
		if (mangaList is null || mangaList.Count < 1)
			return [];

		IEnumerable<string> urls = mangaList.Select(a => $"https://asuracomic.net/{a.GetAttributeValue("href", "")}");
		
		List<(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])> ret = new();
		foreach (string url in urls)
		{
			(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? manga = GetMangaFromUrl(url);
			if (manga is { } x)
				ret.Add(x);
		}

		return ret.ToArray();
	}

	private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[]) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
	{
		string? originalLanguage = null;
		Dictionary<string, string> altTitles = new(), links = new();

		HtmlNodeCollection genreNodes = document.DocumentNode.SelectNodes("//h3[text()='Genres']/../div/button");
		string[] tags = genreNodes.Select(b => b.InnerText).ToArray();
		MangaTag[] mangaTags = tags.Select(t => new MangaTag(t)).ToArray();
		
		HtmlNode statusNode = document.DocumentNode.SelectSingleNode("//h3[text()='Status']/../h3[2]");
		MangaReleaseStatus releaseStatus = statusNode.InnerText.ToLower() switch
		{
			"ongoing" => MangaReleaseStatus.Continuing,
			"hiatus" => MangaReleaseStatus.OnHiatus,
			"completed" => MangaReleaseStatus.Completed,
			"dropped" => MangaReleaseStatus.Cancelled,
			"season end" => MangaReleaseStatus.Continuing,
			"coming soon" => MangaReleaseStatus.Unreleased,
			_ => MangaReleaseStatus.Unreleased
		};
		
		HtmlNode coverNode = 
			document.DocumentNode.SelectSingleNode("//img[@alt='poster']");
		string coverUrl = coverNode.GetAttributeValue("src", "");
		
		HtmlNode titleNode = 
			document.DocumentNode.SelectSingleNode("//title");
		string sortName = Regex.Match(titleNode.InnerText, @"(.*) - Asura Scans").Groups[1].Value;
		
		HtmlNode descriptionNode =
			document.DocumentNode.SelectSingleNode("//h3[starts-with(text(),'Synopsis')]/../span");
		string description = descriptionNode?.InnerText??"";
		
		HtmlNodeCollection authorNodes = document.DocumentNode.SelectNodes("//h3[text()='Author']/../h3[not(text()='Author' or text()='_')]");
		HtmlNodeCollection artistNodes = document.DocumentNode.SelectNodes("//h3[text()='Artist']/../h3[not(text()='Artist' or text()='_')]");
		IEnumerable<string> authorNames = authorNodes is null ? [] : authorNodes.Select(a => a.InnerText);
		IEnumerable<string> artistNames = artistNodes is null ? [] : artistNodes.Select(a => a.InnerText);
		List<string> authorStrings = authorNames.Concat(artistNames).ToList();
		Author[] authors = authorStrings.Select(author => new Author(author)).ToArray();

		HtmlNode? firstChapterNode = document.DocumentNode.SelectSingleNode("//a[contains(@href, 'chapter/1')]/../following-sibling::h3");
		uint year = uint.Parse(firstChapterNode?.InnerText.Split(' ')[^1] ?? "2000");

		Manga manga = new (publicationId, sortName, description, websiteUrl, coverUrl, null, year,
			originalLanguage, releaseStatus, -1, null, null,
			this.Name, 
			authors.Select(a => a.AuthorId).ToArray(), 
			mangaTags.Select(t => t.Tag).ToArray(), 
			[],
			[]);
		
		return (manga, authors, mangaTags, [], []);
	}

	public override Chapter[] GetChapters(Manga manga, string language="en")
	{
		string requestUrl = $"https://asuracomic.net/series/{manga.MangaId}";
		// Leaving this in for verification if the page exists
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return [];

		//Return Chapters ordered by Chapter-Number
		List<Chapter> chapters = ParseChaptersFromHtml(manga, requestUrl);
		return chapters.Order().ToArray();
	}

	private List<Chapter> ParseChaptersFromHtml(Manga manga, string mangaUrl)
	{
		RequestResult result = downloadClient.MakeRequest(mangaUrl, RequestType.Default);
		if ((int)result.statusCode < 200 || (int)result.statusCode >= 300 || result.htmlDocument is null)
		{
			return new List<Chapter>();
		}

		List<Chapter> ret = new();

		HtmlNodeCollection chapterURLNodes = result.htmlDocument.DocumentNode.SelectNodes("//a[contains(@href, '/chapter/')]");
		Regex infoRex = new(@"Chapter ([0-9]+)(.*)?");

		foreach (HtmlNode chapterInfo in chapterURLNodes)
		{
			string chapterUrl = chapterInfo.GetAttributeValue("href", "");

			Match match = infoRex.Match(chapterInfo.InnerText);
			float chapterNumber = float.Parse(match.Groups[1].Value);
			string? chapterName = match.Groups[2].Success && match.Groups[2].Length > 1 ? match.Groups[2].Value : null;
			string url = $"https://asuracomic.net/series/{chapterUrl}";
			try
			{
				ret.Add(new Chapter(manga, url, chapterNumber, null, chapterName));
			}
			catch (Exception e)
			{
			}
		}
		
		return ret;
	}

	internal override string[] GetChapterImageUrls(Chapter chapter)
	{
		string requestUrl = chapter.Url;
		// Leaving this in to check if the page exists
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
		{
			return [];
		}
		string[] imageUrls = ParseImageUrlsFromHtml(requestResult.htmlDocument);
		return imageUrls;
	}

	private string[] ParseImageUrlsFromHtml(HtmlDocument document)
	{
		HtmlNodeCollection images = document.DocumentNode.SelectNodes("//img[contains(@alt, 'chapter page')]");

		return images.Select(i => i.GetAttributeValue("src", "")).ToArray();
	}
}