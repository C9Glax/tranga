using System.Net;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public class Bato : MangaConnector
{
	
	public Bato() : base("Bato", ["en"], ["bato.to"], "https://bato.to/amsta/img/batoto/favicon.ico")
	{
		this.downloadClient = new HttpDownloadClient();
	}

	public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(string publicationTitle = "")
	{
		string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(m => m.Value.Length > 0)).ToLower();
		string requestUrl = $"https://bato.to/v3x-search?word={sanitizedTitle}&lang=en";
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return [];

		if (requestResult.htmlDocument is null)
		{
			return [];
		}
			
		(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
		return publications;
	}

	public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromId(string publicationId)
	{
		return GetMangaFromUrl($"https://bato.to/title/{publicationId}");
	}

	public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromUrl(string url)
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

	private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] ParsePublicationsFromHtml(HtmlDocument document)
	{
		HtmlNode mangaList = document.DocumentNode.SelectSingleNode("//div[@data-hk='0-0-2']");
		if (!mangaList.ChildNodes.Any(node => node.Name == "div"))
			return [];

		List<string> urls = mangaList.ChildNodes
			.Select(node => $"https://bato.to{node.Descendants("div").First().FirstChild.GetAttributeValue("href", "")}").ToList();
		
		HashSet<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)> ret = new();
		foreach (string url in urls)
		{
			(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? manga = GetMangaFromUrl(url);
			if (manga is { } x)
				ret.Add(x);
		}

		return ret.ToArray();
	}

	private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
	{
		HtmlNode infoNode = document.DocumentNode.SelectSingleNode("/html/body/div/main/div[1]/div[2]");

		string sortName = infoNode.Descendants("h3").First().InnerText;
		string description = document.DocumentNode
			.SelectSingleNode("//div[contains(concat(' ',normalize-space(@class),' '),'prose')]").InnerText;

		string[] altTitlesList = infoNode.ChildNodes[1].ChildNodes[2].InnerText.Split('/');
		int i = 0;
		List<MangaAltTitle> altTitles = altTitlesList.Select(a => new MangaAltTitle(i++.ToString(), a)).ToList();

		string coverUrl = document.DocumentNode.SelectNodes("//img")
			.First(child => child.GetAttributeValue("data-hk", "") == "0-1-0").GetAttributeValue("src", "").Replace("&amp;", "&");

		List<HtmlNode> genreNodes = document.DocumentNode.SelectSingleNode("//b[text()='Genres:']/..").SelectNodes("span").ToList();
		string[] tags = genreNodes.Select(node => node.FirstChild.InnerText).ToArray();
		List<MangaTag> mangaTags = tags.Select(s => new MangaTag(s)).ToList();

		List<HtmlNode> authorsNodes = infoNode.ChildNodes[1].ChildNodes[3].Descendants("a").ToList();
		List<string> authorNames = authorsNodes.Select(node => node.InnerText.Replace("amp;", "")).ToList();
		List<Author> authors = authorNames.Select(n => new Author(n)).ToList();

		HtmlNode? originalLanguageNode = document.DocumentNode.SelectSingleNode("//span[text()='Tr From']/..");
		string originalLanguage = originalLanguageNode is not null ? originalLanguageNode.LastChild.InnerText : "";
		
		if (!uint.TryParse(
			    document.DocumentNode.SelectSingleNode("//span[text()='Original Publication:']/..").LastChild.InnerText.Split('-')[0],
			    out uint year))
			year = (uint)DateTime.UtcNow.Year;

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

		Manga manga = new (publicationId, sortName, description, websiteUrl, coverUrl, null, year,
			originalLanguage, releaseStatus, -1,
			this, 
			authors, 
			mangaTags, 
			[],
			altTitles);
		
		return (manga, authors, mangaTags, [], altTitles);
	}

	public override Chapter[] GetChapters(Manga manga, string language="en")
	{
		string requestUrl = $"https://bato.to/title/{manga.MangaId}";
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

		HtmlNode chapterList =
			result.htmlDocument.DocumentNode.SelectSingleNode("/html/body/div/main/div[3]/astro-island/div/div[2]/div/div/astro-slot");

		Regex numberRex = new(@"\/title\/.+\/([0-9])+(?:-vol_([0-9]+))?-ch_([0-9\.]+)");

		foreach (HtmlNode chapterInfo in chapterList.SelectNodes("div"))
		{
			HtmlNode infoNode = chapterInfo.FirstChild.FirstChild;
			string chapterUrl = infoNode.GetAttributeValue("href", "");

			Match match = numberRex.Match(chapterUrl);
			string id = match.Groups[1].Value;
			int? volumeNumber = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : null;
			string chapterNumber = new(match.Groups[3].Value);
			string url = $"https://bato.to{chapterUrl}?load=2";
			try
			{
				ret.Add(new Chapter(manga, url, chapterNumber, volumeNumber, null));
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
		HtmlNode images = document.DocumentNode.SelectNodes("//astro-island").First(node =>
			node.GetAttributeValue("component-url", "").Contains("/_astro/ImageList."));

		string weirdString = images.OuterHtml;
		string weirdString2 = Regex.Match(weirdString, @"props=\""(.*)}\""").Groups[1].Value;
		string[] urls = Regex.Matches(weirdString2, @"(https:\/\/[A-z\-0-9\.\?\&\;\=\/]+)\\")
			.Select(match => match.Groups[1].Value.Replace("&amp;", "&")).ToArray();
		
		return urls;
	}
}