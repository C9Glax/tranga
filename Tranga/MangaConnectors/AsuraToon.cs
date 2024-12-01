using System.Text.RegularExpressions;
using API.Schema;
using HtmlAgilityPack;

namespace Tranga.MangaConnectors;

public class AsuraToon : MangaConnector
{
	//["en"], ["asuracomic.net"]
	public AsuraToon(string mangaConnectorName) : base(mangaConnectorName, new HttpDownloadClient())
	{
	}

	public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] GetManga(string publicationTitle = "")
	{
		log.Info($"Searching Publications. Term=\"{publicationTitle}\"");
		string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(m => m.Value.Length > 0)).ToLower();
		string requestUrl = $"https://asuracomic.net/series?name={sanitizedTitle}";
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return [];

		if (requestResult.htmlDocument is null)
		{
			log.Info($"Failed to retrieve site");
			return [];
		}
			
		(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
		log.Info($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
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
			log.Info($"Failed to retrieve site");
			return null;
		}
		return ParseSinglePublicationFromHtml(requestResult.htmlDocument, url.Split('/')[^1], url);
	}

	private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] ParsePublicationsFromHtml(HtmlDocument document)
	{
		HtmlNodeCollection mangaList = document.DocumentNode.SelectNodes("//a[starts-with(@href,'series')]");
		if (mangaList.Count < 1)
			return [];

		IEnumerable<string> urls = mangaList.Select(a => $"https://asuracomic.net/{a.GetAttributeValue("href", "")}");
		
		List<(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])> ret = new();
		foreach (string url in urls)
		{
			(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? manga = GetMangaFromUrl(url);
			if (manga is not null)
				ret.Add(((Manga, Author[], MangaTag[], Link[], MangaAltTitle[]))manga);
		}

		return ret.ToArray();
	}

	private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[]) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
	{
		string? originalLanguage = null;

		HtmlNodeCollection genreNodes = document.DocumentNode.SelectNodes("//h3[text()='Genres']/../div/button");
		MangaTag[] tags = genreNodes.Select(b => new MangaTag(b.InnerText)).ToArray();
		
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
		string description = descriptionNode.InnerText;
		
		HtmlNodeCollection authorNodes = document.DocumentNode.SelectNodes("//h3[text()='Author']/../h3[not(text()='Author' or text()='_')]");
		HtmlNodeCollection artistNodes = document.DocumentNode.SelectNodes("//h3[text()='Artist']/../h3[not(text()='Author' or text()='_')]");
		Author[] authors = authorNodes.Select(a => new Author(a.InnerText)).Concat(artistNodes.Select(a => new Author(a.InnerText))).ToArray();

		HtmlNode? firstChapterNode = document.DocumentNode.SelectSingleNode("//a[contains(@href, 'chapter/1')]/../following-sibling::h3");
		uint year = uint.Parse(firstChapterNode?.InnerText.Split(' ')[^1] ?? "2000");
		

		Manga manga = new(MangaConnectorName, sortName, description, coverUrl, null, year, originalLanguage,
			releaseStatus, 0, null, null, publicationId, 
			authors.Select(a => a.AuthorId).ToArray(), 
			tags.Select(t => t.Tag).ToArray(), [], []);

		return (manga, authors, tags, [], []);
	}

	public override Chapter[] GetChapters(Manga manga, string language="en")
	{
		log.Info($"Getting chapters {manga}");
		string requestUrl = $"https://asuracomic.net/series/{manga.ConnectorId}";
		// Leaving this in for verification if the page exists
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return Array.Empty<Chapter>();

		//Return Chapters ordered by Chapter-Number
		List<Chapter> chapters = ParseChaptersFromHtml(manga, requestUrl);
		log.Info($"Got {chapters.Count} chapters. {manga}");
		return chapters.Order().ToArray();
	}

	private List<Chapter> ParseChaptersFromHtml(Manga manga, string mangaUrl)
	{
		RequestResult result = downloadClient.MakeRequest(mangaUrl, RequestType.Default);
		if ((int)result.statusCode < 200 || (int)result.statusCode >= 300 || result.htmlDocument is null)
		{
			log.Info("Failed to load site");
			return new List<Chapter>();
		}

		List<Chapter> ret = new();

		HtmlNodeCollection chapterUrlNodes = result.htmlDocument.DocumentNode.SelectNodes("//a[contains(@href, '/chapter/')]");
		Regex infoRex = new(@"Chapter ([0-9]+)(.*)?");

		foreach (HtmlNode chapterInfo in chapterUrlNodes)
		{
			string chapterUrl = chapterInfo.GetAttributeValue("href", "");

			Match match = infoRex.Match(chapterInfo.InnerText);
			string chapterNumber = match.Groups[1].Value;
			float.TryParse(chapterNumber, NumberFormatDecimalPoint, out float chNum);
			string? chapterName = match.Groups[2].Success && match.Groups[2].Length > 1 ? match.Groups[2].Value : null;
			string url = $"https://asuracomic.net/series/{chapterUrl}";
			ret.Add(new Chapter(manga, chapterUrl, chNum, null, chapterName));
		}
		
		return ret;
	}

	protected override string[] GetChapterImages(Chapter chapter)
	{
		Manga chapterParentManga = chapter.ParentManga;
		log.Info($"Retrieving chapter-info {chapter} {chapterParentManga}");
		string requestUrl = chapter.Url;
		// Leaving this in to check if the page exists
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
		{
			return [];
		}

		string[] imageUrls = ParseImageUrlsFromHtml(requestUrl);

		return imageUrls;
	}

	private string[] ParseImageUrlsFromHtml(string mangaUrl)
	{
		RequestResult requestResult =
			downloadClient.MakeRequest(mangaUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
		{
			return [];
		}
		if (requestResult.htmlDocument is null)
		{
			log.Info($"Failed to retrieve site");
			return [];
		}

		HtmlNodeCollection images =
			requestResult.htmlDocument.DocumentNode.SelectNodes("//img[contains(@alt, 'chapter page')]");

		return images.Select(i => i.GetAttributeValue("src", "")).ToArray();
	}
}