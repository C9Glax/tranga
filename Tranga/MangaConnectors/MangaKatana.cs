using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class MangaKatana : MangaConnector
{
	public MangaKatana(GlobalBase clone) : base(clone, "MangaKatana", ["en"])
	{
		this.downloadClient = new HttpDownloadClient(clone);
	}

	public override Manga[] GetManga(string publicationTitle = "")
	{
		Log($"Searching Publications. Term=\"{publicationTitle}\"");
		string sanitizedTitle = string.Join("%20", Regex.Matches(publicationTitle, "[A-z]*").Where(m => m.Value.Length > 0)).ToLower();
		string requestUrl = $"https://mangakatana.com/?search={sanitizedTitle}&search_by=book_name";
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return Array.Empty<Manga>();

		// ReSharper disable once MergeIntoPattern
		// If a single result is found, the user will be redirected to the results directly instead of a result page
		if(requestResult.hasBeenRedirected
		    && requestResult.redirectedToUrl is not null
			&& requestResult.redirectedToUrl.Contains("mangakatana.com/manga"))
		{
			return new [] { ParseSinglePublicationFromHtml(requestResult.result, requestResult.redirectedToUrl.Split('/')[^1], requestResult.redirectedToUrl) };
		}

		Manga[] publications = ParsePublicationsFromHtml(requestResult.result);
		Log($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
		return publications;
	}

	public override Manga? GetMangaFromId(string publicationId)
	{
		return GetMangaFromUrl($"https://mangakatana.com/manga/{publicationId}");
	}

	public override Manga? GetMangaFromUrl(string url)
	{
		RequestResult requestResult =
			downloadClient.MakeRequest(url, RequestType.MangaInfo);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return null;
		return ParseSinglePublicationFromHtml(requestResult.result, url.Split('/')[^1], url);
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
			Manga? manga = GetMangaFromUrl(url);
			if (manga is not null)
				ret.Add((Manga)manga);
		}

		return ret.ToArray();
	}

	private Manga ParseSinglePublicationFromHtml(Stream html, string publicationId, string websiteUrl)
	{
		StreamReader reader = new(html);
		string htmlString = reader.ReadToEnd();
		HtmlDocument document = new();
		document.LoadHtml(htmlString);
		Dictionary<string, string> altTitles = new();
		Dictionary<string, string>? links = null;
		HashSet<string> tags = new();
		string[] authors = Array.Empty<string>();
		string originalLanguage = "";
		Manga.ReleaseStatusByte releaseStatus = Manga.ReleaseStatusByte.Unreleased;

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
					switch (value.ToLower())
					{
						case "ongoing": releaseStatus = Manga.ReleaseStatusByte.Continuing; break;
						case "completed": releaseStatus = Manga.ReleaseStatusByte.Completed; break;
					}
					break;
				case "genres":
					tags = row.SelectNodes("div").Last().Descendants("a").Select(a => a.InnerText).ToHashSet();
					break;
			}
		}

		string posterUrl = document.DocumentNode.SelectSingleNode("//*[@id='single_book']/div[1]/div").Descendants("img").First()
			.GetAttributes().First(a => a.Name == "src").Value;

		string coverFileNameInCache = SaveCoverImageToCache(posterUrl, publicationId, RequestType.MangaCover);

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

		Manga manga = new (sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl, coverFileNameInCache, links,
			year, originalLanguage, publicationId, releaseStatus, websiteUrl: websiteUrl);
		AddMangaToCache(manga);
		return manga;
	}

	public override Chapter[] GetChapters(Manga manga, string language="en")
	{
		Log($"Getting chapters {manga}");
		string requestUrl = $"https://mangakatana.com/manga/{manga.publicationId}";
		// Leaving this in for verification if the page exists
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return Array.Empty<Chapter>();

		//Return Chapters ordered by Chapter-Number
		List<Chapter> chapters = ParseChaptersFromHtml(manga, requestUrl);
		Log($"Got {chapters.Count} chapters. {manga}");
		return chapters.Order().ToArray();
	}

	private List<Chapter> ParseChaptersFromHtml(Manga manga, string mangaUrl)
	{
		// Using HtmlWeb will include the chapters since they are loaded with js 
		HtmlWeb web = new();
		HtmlDocument document = web.Load(mangaUrl);

		List<Chapter> ret = new();

		HtmlNode chapterList = document.DocumentNode.SelectSingleNode("//div[contains(@class, 'chapters')]/table/tbody");

		Regex volumeRex = new(@"[0-9a-z\-\.]+\/[0-9a-z\-]*v([0-9\.]+)");
		Regex chapterNumRex = new(@"[0-9a-z\-\.]+\/[0-9a-z\-]*c([0-9\.]+)");
		Regex chapterNameRex = new(@"Chapter [0-9\.]+:? (.*)");
		
		foreach (HtmlNode chapterInfo in chapterList.Descendants("tr"))
		{
			string fullString = chapterInfo.Descendants("a").First().InnerText;
			string url = chapterInfo.Descendants("a").First()
				.GetAttributeValue("href", "");

			string? volumeNumber = volumeRex.IsMatch(url) ? volumeRex.Match(url).Groups[1].Value : null;
			string chapterNumber = chapterNumRex.Match(url).Groups[1].Value;
			string chapterName = chapterNameRex.Match(fullString).Groups[1].Value;
			ret.Add(new Chapter(manga, chapterName, volumeNumber, chapterNumber, url));
		}
		
		return ret;
	}

	public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
	{
		if (progressToken?.cancellationRequested ?? false)
		{
			progressToken.Cancel();
			return HttpStatusCode.RequestTimeout;
		}

		Manga chapterParentManga = chapter.parentManga;
		Log($"Retrieving chapter-info {chapter} {chapterParentManga}");
		string requestUrl = chapter.url;
		// Leaving this in to check if the page exists
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
		{
			progressToken?.Cancel();
			return requestResult.statusCode;
		}

		string[] imageUrls = ParseImageUrlsFromHtml(requestUrl);

		string comicInfoPath = Path.GetTempFileName();
		File.WriteAllText(comicInfoPath, chapter.GetComicInfoXmlString());

		return DownloadChapterImages(imageUrls, chapter.GetArchiveFilePath(), RequestType.MangaImage, comicInfoPath, "https://mangakatana.com/", progressToken:progressToken);
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