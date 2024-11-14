using System.Text.RegularExpressions;
using API.Schema;
using HtmlAgilityPack;

namespace Tranga.MangaConnectors;

public class Bato : MangaConnector
{
	//["en"], ["bato.to"]
	public Bato(string mangaConnectorId) : base(mangaConnectorId, new HttpDownloadClient())
	{
	}

	public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] GetManga(string publicationTitle = "")
	{
		log.Info($"Searching Publications. Term=\"{publicationTitle}\"");
		string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(m => m.Value.Length > 0)).ToLower();
		string requestUrl = $"https://bato.to/v3x-search?word={sanitizedTitle}&lang=en";
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
		return GetMangaFromUrl($"https://bato.to/title/{publicationId}");
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
		HtmlNode mangaList = document.DocumentNode.SelectSingleNode("//div[@data-hk='0-0-2']");
		if (!mangaList.ChildNodes.Any(node => node.Name == "div"))
			return [];

		List<string> urls = mangaList.ChildNodes
			.Select(node => $"https://bato.to{node.Descendants("div").First().FirstChild.GetAttributeValue("href", "")}").ToList();
		
		HashSet<(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])> ret = new();
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
		HtmlNode infoNode = document.DocumentNode.SelectSingleNode("/html/body/div/main/div[1]/div[2]");

		string sortName = infoNode.Descendants("h3").First().InnerText;
		string description = document.DocumentNode
			.SelectSingleNode("//div[contains(concat(' ',normalize-space(@class),' '),'prose')]").InnerText;

		string[] altTitlesList = infoNode.ChildNodes[1].ChildNodes[2].InnerText.Split('/');
		int i = 0;
		MangaAltTitle[] altTitles = altTitlesList.Select(at => new MangaAltTitle(i++.ToString(), at)).ToArray();

		string posterUrl = document.DocumentNode.SelectNodes("//img")
			.First(child => child.GetAttributeValue("data-hk", "") == "0-1-0").GetAttributeValue("src", "").Replace("&amp;", "&");

		List<HtmlNode> genreNodes = document.DocumentNode.SelectSingleNode("//b[text()='Genres:']/..").SelectNodes("span").ToList();
		MangaTag[] tags = genreNodes.Select(gn => new MangaTag(gn.FirstChild.InnerText)).ToArray();

		List<HtmlNode> authorsNodes = infoNode.ChildNodes[1].ChildNodes[3].Descendants("a").ToList();
		Author[] authors = authorsNodes.Select(an => new Author(an.InnerText.Replace("amp;", ""))).ToArray();

		HtmlNode? originalLanguageNode = document.DocumentNode.SelectSingleNode("//span[text()='Tr From']/..");
		string originalLanguage = originalLanguageNode is not null ? originalLanguageNode.LastChild.InnerText : "";

		uint year = uint.Parse(document.DocumentNode.SelectSingleNode("//span[text()='Original Publication:']/..").LastChild
			.InnerText.Split('-')[0] ?? "0");

		string status = document.DocumentNode.SelectSingleNode("//span[text()='Original Publication:']/..")
			.ChildNodes[2].InnerText;
		MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;
		switch (status.ToLower())
		{
			case "ongoing": releaseStatus = MangaReleaseStatus.Continuing; break;
			case "completed": releaseStatus = MangaReleaseStatus.Completed; break;
			case "hiatus": releaseStatus = MangaReleaseStatus.OnHiatus; break;
			case "cancelled": releaseStatus = MangaReleaseStatus.Cancelled; break;
			case "pending": releaseStatus = MangaReleaseStatus.Unreleased; break;
		}

		Manga manga = new(MangaConnectorId, sortName, description, posterUrl, null,
			year, originalLanguage, releaseStatus, 0, null, null,
			publicationId,
			authors.Select(a => a.AuthorId).ToArray(),
			tags.Select(t => t.Tag).ToArray(),
			[], altTitles.Select(at => at.AltTitleId).ToArray());
		return (manga, authors, tags, [], altTitles);
	}

	public override Chapter[] GetChapters(Manga manga, string language="en")
	{
		log.Info($"Getting chapters {manga}");
		string requestUrl = $"https://bato.to/title/{manga.MangaConnectorId}";
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

		HtmlNode chapterList =
			result.htmlDocument.DocumentNode.SelectSingleNode("/html/body/div/main/div[3]/astro-island/div/div[2]/div/div/astro-slot");

		Regex numberRex = new(@"\/title\/.+\/([0-9])+(?:-vol_([0-9]+))?-ch_([0-9\.]+)");

		foreach (HtmlNode chapterInfo in chapterList.SelectNodes("div"))
		{
			HtmlNode infoNode = chapterInfo.FirstChild.FirstChild;
			string chapterUrl = infoNode.GetAttributeValue("href", "");

			Match match = numberRex.Match(chapterUrl);
			string id = match.Groups[1].Value;
			string? volumeNumber = match.Groups[2].Success ? match.Groups[2].Value : null;
			float.TryParse(volumeNumber, NumberFormatDecimalPoint, out float volNum);
			string chapterNumber = match.Groups[3].Value;
			if (!float.TryParse(chapterNumber, NumberFormatDecimalPoint, out float chNum))
			{
				log.Debug($"Failed parsing {chapterNumber} as float.");
				continue;
			}
			string url = $"https://bato.to{chapterUrl}?load=2";
			ret.Add(new Chapter(manga, url, chNum, volNum, null));
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
			return Array.Empty<string>();
		}
		if (requestResult.htmlDocument is null)
		{
			log.Info($"Failed to retrieve site");
			return Array.Empty<string>();
		}

		HtmlDocument document = requestResult.htmlDocument;

		HtmlNode images = document.DocumentNode.SelectNodes("//astro-island").First(node =>
			node.GetAttributeValue("component-url", "").Contains("/_astro/ImageList."));

		string weirdString = images.OuterHtml;
		string weirdString2 = Regex.Match(weirdString, @"props=\""(.*)}\""").Groups[1].Value;
		string[] urls = Regex.Matches(weirdString2, @"(https:\/\/[A-z\-0-9\.\?\&\;\=\/]+)\\")
			.Select(match => match.Groups[1].Value.Replace("&amp;", "&")).ToArray();
		
		return urls;
	}
}