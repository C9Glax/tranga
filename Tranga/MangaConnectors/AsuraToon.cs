using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class AsuraToon : MangaConnector
{
	
	public AsuraToon(GlobalBase clone) : base(clone, "AsuraToon", ["en"], ["asuracomic.net"])
	{
		this.downloadClient = new ChromiumDownloadClient(clone);
	}

	public override Manga[] GetManga(string publicationTitle = "")
	{
		Log($"Searching Publications. Term=\"{publicationTitle}\"");
		string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(m => m.Value.Length > 0)).ToLower();
		string requestUrl = $"https://asuracomic.net/series?name={sanitizedTitle}";
		RequestResult requestResult =
			downloadClient.MakeRequest(requestUrl, RequestType.Default);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return Array.Empty<Manga>();

		if (requestResult.htmlDocument is null)
		{
			Log($"Failed to retrieve site");
			return Array.Empty<Manga>();
		}
			
		Manga[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
		Log($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
		return publications;
	}

	public override Manga? GetMangaFromId(string publicationId)
	{
		return GetMangaFromUrl($"https://asuracomic.net/series/{publicationId}");
	}

	public override Manga? GetMangaFromUrl(string url)
	{
		RequestResult requestResult = downloadClient.MakeRequest(url, RequestType.MangaInfo);
		if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
			return null;
		if (requestResult.htmlDocument is null)
		{
			Log($"Failed to retrieve site");
			return null;
		}
		return ParseSinglePublicationFromHtml(requestResult.htmlDocument, url.Split('/')[^1], url);
	}

	private Manga[] ParsePublicationsFromHtml(HtmlDocument document)
	{
		HtmlNodeCollection mangaList = document.DocumentNode.SelectNodes("//a[starts-with(@href,'series')]");
		if (mangaList is null || mangaList.Count < 1)
			return [];

		IEnumerable<string> urls = mangaList.Select(a => $"https://asuracomic.net/{a.GetAttributeValue("href", "")}");
		
		List<Manga> ret = new();
		foreach (string url in urls)
		{
			Manga? manga = GetMangaFromUrl(url);
			if (manga is not null)
				ret.Add((Manga)manga);
		}

		return ret.ToArray();
	}

	private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
	{
		string? originalLanguage = null;
		Dictionary<string, string> altTitles = new(), links = new();

		HtmlNodeCollection genreNodes = document.DocumentNode.SelectNodes("//h3[text()='Genres']/../div/button");
		string[] tags = genreNodes.Select(b => b.InnerText).ToArray();
		
		HtmlNode statusNode = document.DocumentNode.SelectSingleNode("//h3[text()='Status']/../h3[2]");
		Manga.ReleaseStatusByte releaseStatus = statusNode.InnerText.ToLower() switch
		{
			"ongoing" => Manga.ReleaseStatusByte.Continuing,
			"hiatus" => Manga.ReleaseStatusByte.OnHiatus,
			"completed" => Manga.ReleaseStatusByte.Completed,
			"dropped" => Manga.ReleaseStatusByte.Cancelled,
			"season end" => Manga.ReleaseStatusByte.Continuing,
			"coming soon" => Manga.ReleaseStatusByte.Unreleased,
			_ => Manga.ReleaseStatusByte.Unreleased
		};
		
		HtmlNode coverNode = 
			document.DocumentNode.SelectSingleNode("//img[@alt='poster']");
		string coverUrl = coverNode.GetAttributeValue("src", "");
		string coverFileNameInCache = SaveCoverImageToCache(coverUrl, publicationId, RequestType.MangaCover);
		
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
		List<string> authors = authorNames.Concat(artistNames).ToList();

		HtmlNode? firstChapterNode = document.DocumentNode.SelectSingleNode("//a[contains(@href, 'chapter/1')]/../following-sibling::h3");
		int? year = int.Parse(firstChapterNode?.InnerText.Split(' ')[^1] ?? "2000");
		
		Manga manga = new (this, sortName, authors, description, altTitles, tags, coverUrl, coverFileNameInCache, links,
			year, originalLanguage, publicationId, releaseStatus, websiteUrl);
		AddMangaToCache(manga);
		return manga;
	}

	public override Chapter[] GetChapters(Manga manga, string language="en")
	{
		Log($"Getting chapters {manga}");
		string requestUrl = $"https://asuracomic.net/series/{manga.publicationId}";
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
		RequestResult result = downloadClient.MakeRequest(mangaUrl, RequestType.Default);
		if ((int)result.statusCode < 200 || (int)result.statusCode >= 300 || result.htmlDocument is null)
		{
			Log("Failed to load site");
			return new List<Chapter>();
		}

		List<Chapter> ret = new();

		HtmlNodeCollection chapterURLNodes = result.htmlDocument.DocumentNode.SelectNodes("//a[contains(@href, '/chapter/')]");
		Regex infoRex = new(@"Chapter ([0-9]+)(.*)?");

		foreach (HtmlNode chapterInfo in chapterURLNodes)
		{
			string chapterUrl = chapterInfo.GetAttributeValue("href", "");

			Match match = infoRex.Match(chapterInfo.InnerText);
			string chapterNumber = match.Groups[1].Value;
			string? chapterName = match.Groups[2].Success && match.Groups[2].Length > 1 ? match.Groups[2].Value : null;
			string url = $"https://asuracomic.net/series/{chapterUrl}";
			try
			{
				ret.Add(new Chapter(manga, chapterName, null, chapterNumber, url));
			}
			catch (Exception e)
			{
				Log($"Failed to load chapter {chapterNumber}: {e.Message}");
			}
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
		
		return DownloadChapterImages(imageUrls, chapter, RequestType.MangaImage, progressToken:progressToken);
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
			Log($"Failed to retrieve site");
			return Array.Empty<string>();
		}

		HtmlNodeCollection images =
			requestResult.htmlDocument.DocumentNode.SelectNodes("//img[contains(@alt, 'chapter page')]");

		return images.Select(i => i.GetAttributeValue("src", "")).ToArray();
	}
}