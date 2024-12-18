using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public class MangaKatana : MangaConnector
{
	public MangaKatana() : base("MangaKatana", ["en"], ["mangakatana.com"])
	{
		this.downloadClient = new HttpDownloadClient();
	}

	public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(string publicationTitle = "")
	{
		string sanitizedTitle = string.Join("%20", Regex.Matches(publicationTitle, "[A-z]*").Where(m => m.Value.Length > 0)).ToLower();
		string requestUrl = $"https://mangakatana.com/?search={sanitizedTitle}&search_by=book_name";
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return [];

		// ReSharper disable once MergeIntoPattern
		// If a single result is found, the user will be redirected to the results directly instead of a result page
		if(requestResult.hasBeenRedirected
		    && requestResult.redirectedToUrl is not null
			&& requestResult.redirectedToUrl.Contains("mangakatana.com/manga"))
		{
			return new [] { ParseSinglePublicationFromHtml(requestResult.result, requestResult.redirectedToUrl.Split('/')[^1], requestResult.redirectedToUrl) };
		}

		(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] publications = ParsePublicationsFromHtml(requestResult.result);
		return publications;
	}

	public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromId(string publicationId)
	{
		return GetMangaFromUrl($"https://mangakatana.com/manga/{publicationId}");
	}

	public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromUrl(string url)
	{
		RequestResult requestResult =
			downloadClient.MakeRequest(url, RequestType.MangaInfo);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return null;
		return ParseSinglePublicationFromHtml(requestResult.result, url.Split('/')[^1], url);
	}

	private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] ParsePublicationsFromHtml(Stream html)
	{
		StreamReader reader = new(html);
		string htmlString = reader.ReadToEnd();
		HtmlDocument document = new();
		document.LoadHtml(htmlString);
		IEnumerable<HtmlNode> searchResults = document.DocumentNode.SelectNodes("//*[@id='book_list']/div");
		if (searchResults is null || !searchResults.Any())
			return [];
		List<string> urls = new();
		foreach (HtmlNode mangaResult in searchResults)
		{
			urls.Add(mangaResult.Descendants("a").First().GetAttributes()
				.First(a => a.Name == "href").Value);
		}

		HashSet<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)> ret = new();
		foreach (string url in urls)
		{
			(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? manga = GetMangaFromUrl(url);
			if (manga is { } x)
				ret.Add(x);
		}

		return ret.ToArray();
	}

	private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?) ParseSinglePublicationFromHtml(Stream html, string publicationId, string websiteUrl)
	{
		StreamReader reader = new(html);
		string htmlString = reader.ReadToEnd();
		HtmlDocument document = new();
		document.LoadHtml(htmlString);
		Dictionary<string, string> altTitlesDict = new();
		Dictionary<string, string>? links = null;
		HashSet<string> tags = new();
		string[] authorNames = [];
		string originalLanguage = "";
		MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

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
						altTitlesDict.Add(i.ToString(), alts[i]);
					break;
				case "authorsartists":
					authorNames = value.Split(',');
					break;
				case "status":
					switch (value.ToLower())
					{
						case "ongoing": releaseStatus = MangaReleaseStatus.Continuing; break;
						case "completed": releaseStatus = MangaReleaseStatus.Completed; break;
					}
					break;
				case "genres":
					tags = row.SelectNodes("div").Last().Descendants("a").Select(a => a.InnerText).ToHashSet();
					break;
			}
		}

		string coverUrl = document.DocumentNode.SelectSingleNode("//*[@id='single_book']/div[1]/div").Descendants("img").First()
			.GetAttributes().First(a => a.Name == "src").Value;

		string description = document.DocumentNode.SelectSingleNode("//*[@id='single_book']/div[3]/p").InnerText;
		while (description.StartsWith('\n'))
			description = description.Substring(1);

		uint year = (uint)DateTime.Now.Year;
		string yearString = infoTable.Descendants("div").First(d => d.HasClass("updateAt"))
			.InnerText.Split('-')[^1];

		if(yearString.Contains("ago") == false)
		{
			year = uint.Parse(yearString);
		}
		List<Author> authors = authorNames.Select(n => new Author(n)).ToList();
		List<MangaTag> mangaTags = tags.Select(n => new MangaTag(n)).ToList();
		List<MangaAltTitle> altTitles = altTitlesDict.Select(x => new MangaAltTitle(x.Key, x.Value)).ToList();

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
		string requestUrl = $"https://mangakatana.com/manga/{manga.MangaId}";
		// Leaving this in for verification if the page exists
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return Array.Empty<Chapter>();

		//Return Chapters ordered by Chapter-Number
		List<Chapter> chapters = ParseChaptersFromHtml(manga, requestUrl);
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

			float? volumeNumber = volumeRex.IsMatch(url) ? float.Parse(volumeRex.Match(url).Groups[1].Value) : null;
			float chapterNumber = float.Parse(chapterNumRex.Match(url).Groups[1].Value);
			string chapterName = chapterNameRex.Match(fullString).Groups[1].Value;
			try
			{
				ret.Add(new Chapter(manga, url, chapterNumber, volumeNumber, chapterName));
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