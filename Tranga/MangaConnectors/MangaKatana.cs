using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class MangaKatana : MangaConnector
{
	public override string name { get; }

	public MangaKatana(GlobalBase clone) : base(clone)
	{
		this.name = "MangaKatana";
		this.downloadClient = new DownloadClient(clone, new Dictionary<byte, int>()
		{
			{1, 60}
		});
	}

	public override Manga[] GetPublications(string publicationTitle = "")
	{
		Log($"Searching Publications. Term=\"{publicationTitle}\"");
		string sanitizedTitle = string.Join('_', Regex.Matches(publicationTitle, "[A-z]*").Where(m => m.Value.Length > 0)).ToLower();
		string requestUrl = $"https://mangakatana.com/?search={sanitizedTitle}&search_by=book_name";
		DownloadClient.RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, 1);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return Array.Empty<Manga>();

		// ReSharper disable once MergeIntoPattern
		// If a single result is found, the user will be redirected to the results directly instead of a result page
		if(requestResult.hasBeenRedirected
		    && requestResult.redirectedToUrl is not null
			&& requestResult.redirectedToUrl.Contains("mangakatana.com/manga"))
		{
			return new [] { ParseSinglePublicationFromHtml(requestResult.result, requestResult.redirectedToUrl.Split('/')[^1]) };
		}

		Manga[] publications = ParsePublicationsFromHtml(requestResult.result);
		cachedPublications.AddRange(publications);
		Log($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
		return publications;
	}

	private Manga[] ParsePublicationsFromHtml(Stream html)
	{
		StreamReader reader = new(html);
		string htmlString = reader.ReadToEnd();
		HtmlDocument document = new();
		document.LoadHtml(htmlString);
		IEnumerable<HtmlNode> searchResults = document.DocumentNode.SelectNodes("//*[@id='book_list']/div");
		if (searchResults is null || !searchResults.Any())
			return Array.Empty<Manga>();
		List<string> urls = new();
		foreach (HtmlNode mangaResult in searchResults)
		{
			urls.Add(mangaResult.Descendants("a").First().GetAttributes()
				.First(a => a.Name == "href").Value);
		}

		HashSet<Manga> ret = new();
		foreach (string url in urls)
		{
			DownloadClient.RequestResult requestResult =
				downloadClient.MakeRequest(url, 1);
			if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
				return Array.Empty<Manga>();

			ret.Add(ParseSinglePublicationFromHtml(requestResult.result, url.Split('/')[^1]));
		}

		return ret.ToArray();
	}

	private Manga ParseSinglePublicationFromHtml(Stream html, string publicationId)
	{
		StreamReader reader = new(html);
		string htmlString = reader.ReadToEnd();
		HtmlDocument document = new();
		document.LoadHtml(htmlString);
		string status = "";
		Dictionary<string, string> altTitles = new();
		Dictionary<string, string>? links = null;
		HashSet<string> tags = new();
		string[] authors = Array.Empty<string>();
		string originalLanguage = "";

		HtmlNode infoNode = document.DocumentNode.SelectSingleNode("//*[@id='single_book']");
		string sortName = infoNode.Descendants("h1").First(n => n.HasClass("heading")).InnerText;
		HtmlNode infoTable = infoNode.SelectSingleNode("//*[@id='single_book']/div[2]/div/ul");

		foreach (HtmlNode row in infoTable.Descendants("li"))
		{
			string key = row.SelectNodes("div").First().InnerText.ToLower();
			string value = row.SelectNodes("div").Last().InnerText;
			string keySanitized = string.Concat(Regex.Matches(key, "[a-z]"));

			switch (keySanitized)
			{
				case "altnames":
					string[] alts = value.Split(" ; ");
					for (int i = 0; i < alts.Length; i++)
						altTitles.Add(i.ToString(), alts[i]);
					break;
				case "authorsartists":
					authors = value.Split(',');
					break;
				case "status":
					status = value;
					break;
				case "genres":
					tags = row.SelectNodes("div").Last().Descendants("a").Select(a => a.InnerText).ToHashSet();
					break;
			}
		}

		string posterUrl = document.DocumentNode.SelectSingleNode("//*[@id='single_book']/div[1]/div").Descendants("img").First()
			.GetAttributes().First(a => a.Name == "src").Value;

		string coverFileNameInCache = SaveCoverImageToCache(posterUrl, 1);

		string description = document.DocumentNode.SelectSingleNode("//*[@id='single_book']/div[3]/p").InnerText;
		while (description.StartsWith('\n'))
			description = description.Substring(1);

		int year = DateTime.Now.Year;
		string yearString = infoTable.Descendants("div").First(d => d.HasClass("updateAt"))
			.InnerText.Split('-')[^1];

		if(yearString.Contains("ago") == false)
		{
			year = Convert.ToInt32(yearString);
		}

		return new Manga(sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl, coverFileNameInCache, links,
			year, originalLanguage, status, publicationId);
	}

	public override Chapter[] GetChapters(Manga manga, string language="en")
	{
		Log($"Getting chapters {manga}");
		string requestUrl = $"https://mangakatana.com/manga/{manga.publicationId}";
		// Leaving this in for verification if the page exists
		DownloadClient.RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, 1);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return Array.Empty<Chapter>();

		//Return Chapters ordered by Chapter-Number
		NumberFormatInfo chapterNumberFormatInfo = new()
		{
			NumberDecimalSeparator = "."
		};
		List<Chapter> chapters = ParseChaptersFromHtml(manga, requestUrl);
		Log($"Got {chapters.Count} chapters. {manga}");
		return chapters.OrderBy(chapter => Convert.ToSingle(chapter.chapterNumber, chapterNumberFormatInfo)).ToArray();
	}

	private List<Chapter> ParseChaptersFromHtml(Manga manga, string mangaUrl)
	{
		// Using HtmlWeb will include the chapters since they are loaded with js 
		HtmlWeb web = new();
		HtmlDocument document = web.Load(mangaUrl);

		List<Chapter> ret = new();

		HtmlNode chapterList = document.DocumentNode.SelectSingleNode("//div[contains(@class, 'chapters')]/table/tbody");

		foreach (HtmlNode chapterInfo in chapterList.Descendants("tr"))
		{
			string fullString = chapterInfo.Descendants("a").First().InnerText;

			string? volumeNumber = fullString.Contains("Vol.") ? fullString.Replace("Vol.", "").Split(' ')[0] : null;
			string chapterNumber = fullString.Split(':')[0].Split("Chapter ")[1].Split(" ")[0].Replace('-', '.');
			string chapterName = string.Concat(fullString.Split(':')[1..]);
			string url = chapterInfo.Descendants("a").First()
				.GetAttributeValue("href", "");
			ret.Add(new Chapter(manga, chapterName, volumeNumber, chapterNumber, url));
		}
		
		return ret;
	}

	public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
	{
		if (progressToken?.cancellationRequested ?? false)
			return HttpStatusCode.RequestTimeout;
		Manga chapterParentManga = chapter.parentManga;
		Log($"Retrieving chapter-info {chapter} {chapterParentManga}");
		string requestUrl = chapter.url;
		// Leaving this in to check if the page exists
		DownloadClient.RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, 1);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return requestResult.statusCode;

		string[] imageUrls = ParseImageUrlsFromHtml(requestUrl);

		string comicInfoPath = Path.GetTempFileName();
		File.WriteAllText(comicInfoPath, chapter.GetComicInfoXmlString());

		return DownloadChapterImages(imageUrls, chapter.GetArchiveFilePath(settings.downloadLocation), 1, comicInfoPath, "https://mangakatana.com/", progressToken:progressToken);
	}

	private string[] ParseImageUrlsFromHtml(string mangaUrl)
	{
		HtmlWeb web = new();
		HtmlDocument document = web.Load(mangaUrl);

		// Images are loaded dynamically, but the urls are present in a piece of js code on the page
		string js = document.DocumentNode.SelectSingleNode("//script[contains(., 'data-src')]").InnerText
			.Replace("\r", "")
			.Replace("\n", "")
			.Replace("\t", "");
		
		// ReSharper disable once StringLiteralTypo
		string regexPat = @"(var thzq=\[')(.*)(,];function)";
		var group = Regex.Matches(js, regexPat).First().Groups[2].Value.Replace("'", "");
		var urls = group.Split(',');
		
		return urls;
	}
}