using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class ManhuaPlus : MangaConnector
{
    public ManhuaPlus(GlobalBase clone) : base(clone, "ManhuaPlus")
    {
        this.downloadClient = new ChromiumDownloadClient(clone);
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0)).ToLower();
        string requestUrl = $"https://manhuaplus.org/search?keyword={sanitizedTitle}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Manga>();

        if (requestResult.htmlDocument is null)
            return Array.Empty<Manga>();
        Manga[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        Log($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
        return publications;
    }
    
    private Manga[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        if (document.DocumentNode.SelectSingleNode("//h1/../..").ChildNodes//I already want to not.
                .Any(node => node.InnerText.Contains("No manga found")))
            return Array.Empty<Manga>();

        List<string> urls = document.DocumentNode
            .SelectNodes("//h1/../..//a[contains(@href, 'https://manhuaplus.org/manga/') and contains(concat(' ',normalize-space(@class),' '),' clamp ') and not(contains(@href, '/chapter'))]")
                .Select(mangaNode => mangaNode.GetAttributeValue("href", "")).ToList();
        logger?.WriteLine($"Got {urls.Count} urls.");

        HashSet<Manga> ret = new();
        foreach (string url in urls)
        {
            Manga? manga = GetMangaFromUrl(url);
            if (manga is not null)
                ret.Add((Manga)manga);
        }

        return ret.ToArray();
    }

    public override Manga? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://manhuaplus.org/manga/{publicationId}");
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        Regex publicationIdRex = new(@"https:\/\/manhuaplus.org\/manga\/(.*)(\/.*)*");
        string publicationId = publicationIdRex.Match(url).Groups[1].Value;

        RequestResult requestResult = this.downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if((int)requestResult.statusCode < 300 && (int)requestResult.statusCode >= 200 && requestResult.htmlDocument is not null && requestResult.redirectedToUrl != "https://manhuaplus.org/home") //When manga doesnt exists it redirects to home
            return ParseSinglePublicationFromHtml(requestResult.htmlDocument, publicationId, url);
        return null;
    }

    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        string originalLanguage = "", status = "";
        Dictionary<string, string> altTitles = new(), links = new();
        HashSet<string> tags = new();
        Manga.ReleaseStatusByte releaseStatus = Manga.ReleaseStatusByte.Unreleased;

        HtmlNode posterNode = document.DocumentNode.SelectSingleNode("/html/body/main/div/div/div[2]/div[1]/figure/a/img");//BRUH
        Regex posterRex = new(@".*(\/uploads/covers/[a-zA-Z0-9\-\._\~\!\$\&\'\(\)\*\+\,\;\=\:\@]+).*");
        string posterUrl = $"https://manhuaplus.org/{posterRex.Match(posterNode.GetAttributeValue("src", "")).Groups[1].Value}";
        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, publicationId, RequestType.MangaCover);

        HtmlNode titleNode = document.DocumentNode.SelectSingleNode("//h1");
        string sortName = titleNode.InnerText.Replace("\n", "");
        
        List<string> authors = new();
        try
        {
            HtmlNode[] authorsNodes = document.DocumentNode
                .SelectNodes("//a[contains(@href, 'https://manhuaplus.org/authors/')]")
                .ToArray();
            foreach (HtmlNode authorNode in authorsNodes)
                authors.Add(authorNode.InnerText);
        }
        catch (ArgumentNullException e)
        {
            Log("No authors found.");
        }

        try
        {
            HtmlNode[] genreNodes = document.DocumentNode
                .SelectNodes("//a[contains(@href, 'https://manhuaplus.org/genres/')]").ToArray();
            foreach (HtmlNode genreNode in genreNodes)
                tags.Add(genreNode.InnerText.Replace("\n", ""));
        }
        catch (ArgumentNullException e)
        {
            Log("No genres found");
        }

        string yearNodeStr = document.DocumentNode
            .SelectSingleNode("//aside//i[contains(concat(' ',normalize-space(@class),' '),' fa-clock ')]/../span").InnerText.Replace("\n", "");
        int year = int.Parse(yearNodeStr.Split(' ')[0].Split('/')[^1]);

        status = document.DocumentNode.SelectSingleNode("//aside//i[contains(concat(' ',normalize-space(@class),' '),' fa-rss ')]/../span").InnerText.Replace("\n", "");
        switch (status.ToLower())
        {
            case "cancelled": releaseStatus = Manga.ReleaseStatusByte.Cancelled; break;
            case "hiatus": releaseStatus = Manga.ReleaseStatusByte.OnHiatus; break;
            case "discontinued": releaseStatus = Manga.ReleaseStatusByte.Cancelled; break;
            case "complete": releaseStatus = Manga.ReleaseStatusByte.Completed; break;
            case "ongoing": releaseStatus = Manga.ReleaseStatusByte.Continuing; break;
        }

        HtmlNode descriptionNode = document.DocumentNode
            .SelectSingleNode("//div[@id='syn-target']");
        string description = descriptionNode.InnerText;

        Manga manga = new(sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl,
            coverFileNameInCache, links,
            year, originalLanguage, publicationId, releaseStatus, websiteUrl: websiteUrl);
        AddMangaToCache(manga);
        return manga;
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        Log($"Getting chapters {manga}");
        RequestResult result = downloadClient.MakeRequest($"https://manhuaplus.org/manga/{manga.publicationId}", RequestType.Default);
        if ((int)result.statusCode < 200 || (int)result.statusCode >= 300 || result.htmlDocument is null)
        {
            return Array.Empty<Chapter>();
        }
        
        HtmlNodeCollection chapterNodes = result.htmlDocument.DocumentNode.SelectNodes("//li[contains(concat(' ',normalize-space(@class),' '),' chapter ')]//a");
        string[] urls = chapterNodes.Select(node => node.GetAttributeValue("href", "")).ToArray();
        Regex urlRex = new (@".*\/chapter-([0-9\-]+).*");
        
        List<Chapter> chapters = new();
        foreach (string url in urls)
        {
            Match rexMatch = urlRex.Match(url);

            string volumeNumber = "1";
            string chapterNumber = rexMatch.Groups[1].Value;
            string fullUrl = url;
            chapters.Add(new Chapter(manga, "", volumeNumber, chapterNumber, fullUrl));
        }
        //Return Chapters ordered by Chapter-Number
        Log($"Got {chapters.Count} chapters. {manga}");
        return chapters.Order().ToArray();
    }

    public override HttpStatusCode DownloadChapter(Chapter chapter, ProgressToken? progressToken = null)
    {
        if (progressToken?.cancellationRequested ?? false)
        {
            progressToken.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Manga chapterParentManga = chapter.parentManga;
        if (progressToken?.cancellationRequested ?? false)
        {
            progressToken.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        Log($"Retrieving chapter-info {chapter} {chapterParentManga}");

        RequestResult requestResult = this.downloadClient.MakeRequest(chapter.url, RequestType.Default);
        if (requestResult.htmlDocument is null)
        {
            progressToken?.Cancel();
            return HttpStatusCode.RequestTimeout;
        }

        HtmlDocument document = requestResult.htmlDocument;
        
        HtmlNode[] images = document.DocumentNode.SelectNodes("//a[contains(concat(' ',normalize-space(@class),' '),' readImg ')]/img").ToArray();
        List<string> urls = images.Select(node => node.GetAttributeValue("src", "")).ToList();
            
        string comicInfoPath = Path.GetTempFileName();
        File.WriteAllText(comicInfoPath, chapter.GetComicInfoXmlString());
        
        return DownloadChapterImages(urls.ToArray(), chapter.GetArchiveFilePath(), RequestType.MangaImage, comicInfoPath, progressToken:progressToken);
    }
}