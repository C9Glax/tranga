using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Tranga.Jobs;

namespace Tranga.MangaConnectors;

public class Mangaworld: MangaConnector
{
    public Mangaworld(GlobalBase clone) : base(clone, "Mangaworld")
    {
        this.downloadClient = new HttpDownloadClient(clone);
    }

    public override Manga[] GetManga(string publicationTitle = "")
    {
        Log($"Searching Publications. Term=\"{publicationTitle}\"");
        string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0)).ToLower();
        string requestUrl = $"https://www.mangaworld.ac/archive?keyword={sanitizedTitle}";
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
        if (!document.DocumentNode.SelectSingleNode("//div[@class='comics-grid']").ChildNodes
                .Any(node => node.HasClass("entry")))
            return Array.Empty<Manga>();
        
        List<string> urls = document.DocumentNode
            .SelectNodes(
                "//div[@class='comics-grid']//div[@class='entry']//a[contains(concat(' ',normalize-space(@class),' '),'thumb')]")
            .Select(thumb => thumb.GetAttributeValue("href", "")).ToList();

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
        return GetMangaFromUrl($"https://www.mangaworld.ac/manga/{publicationId}");
    }

    public override Manga? GetMangaFromUrl(string url)
    {
        RequestResult requestResult =
            downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return null;
        
        if (requestResult.htmlDocument is null)
            return null;
        
        Regex idRex = new (@"https:\/\/www\.mangaworld\.[a-z]{0,63}\/manga\/([0-9]+\/[0-9A-z\-]+).*");
        string id = idRex.Match(url).Groups[1].Value;
        return ParseSinglePublicationFromHtml(requestResult.htmlDocument, id, url);
    }

    private Manga ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        Dictionary<string, string> altTitles = new();
        Dictionary<string, string>? links = null;
        string originalLanguage = "";
        Manga.ReleaseStatusByte releaseStatus = Manga.ReleaseStatusByte.Unreleased;

        HtmlNode infoNode = document.DocumentNode.Descendants("div").First(d => d.HasClass("info"));

        string sortName = infoNode.Descendants("h1").First().InnerText;

        HtmlNode metadata = infoNode.Descendants().First(d => d.HasClass("meta-data"));

        HtmlNode altTitlesNode = metadata.SelectSingleNode("//span[text()='Titoli alternativi: ' or text()='Titolo alternativo: ']/..").ChildNodes[1];

        string[] alts = altTitlesNode.InnerText.Split(", ");
        for(int i = 0; i < alts.Length; i++)
            altTitles.Add(i.ToString(), alts[i]);

        HtmlNode genresNode =
            metadata.SelectSingleNode("//span[text()='Generi: ' or text()='Genero: ']/..");
        HashSet<string> tags = genresNode.SelectNodes("a").Select(node => node.InnerText).ToHashSet();
        
        HtmlNode authorsNode =
            metadata.SelectSingleNode("//span[text()='Autore: ' or text()='Autori: ']/..");
        string[] authors = authorsNode.SelectNodes("a").Select(node => node.InnerText).ToArray();

        string status = metadata.SelectSingleNode("//span[text()='Stato: ']/..").SelectNodes("a").First().InnerText;
        // ReSharper disable 5 times StringLiteralTypo
        switch (status.ToLower())
        {
            case "cancellato": releaseStatus = Manga.ReleaseStatusByte.Cancelled; break;
            case "in pausa": releaseStatus = Manga.ReleaseStatusByte.OnHiatus; break;
            case "droppato": releaseStatus = Manga.ReleaseStatusByte.Cancelled; break;
            case "finito": releaseStatus = Manga.ReleaseStatusByte.Completed; break;
            case "in corso": releaseStatus = Manga.ReleaseStatusByte.Continuing; break;
        }

        string posterUrl = document.DocumentNode.SelectSingleNode("//img[@class='rounded']").GetAttributeValue("src", "");

        string coverFileNameInCache = SaveCoverImageToCache(posterUrl, publicationId.Replace('/', '-'), RequestType.MangaCover);

        string description = document.DocumentNode.SelectSingleNode("//div[@id='noidungm']").InnerText;
        
        string yearString = metadata.SelectSingleNode("//span[text()='Anno di uscita: ']/..").SelectNodes("a").First().InnerText;
        int year = Convert.ToInt32(yearString);
        
        Manga manga = new (sortName, authors.ToList(), description, altTitles, tags.ToArray(), posterUrl, coverFileNameInCache, links,
            year, originalLanguage, publicationId, releaseStatus, websiteUrl: websiteUrl);
        AddMangaToCache(manga);
        return manga;
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        Log($"Getting chapters {manga}");
        string requestUrl = $"https://www.mangaworld.ac/manga/{manga.publicationId}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Chapter>();
        
        //Return Chapters ordered by Chapter-Number
        if (requestResult.htmlDocument is null)
            return Array.Empty<Chapter>();
        List<Chapter> chapters = ParseChaptersFromHtml(manga, requestResult.htmlDocument);
        Log($"Got {chapters.Count} chapters. {manga}");
        return chapters.Order().ToArray();
    }

    private List<Chapter> ParseChaptersFromHtml(Manga manga, HtmlDocument document)
    {
        List<Chapter> ret = new();

        HtmlNode chaptersWrapper =
            document.DocumentNode.SelectSingleNode(
                "//div[contains(concat(' ',normalize-space(@class),' '),'chapters-wrapper')]");

        if (chaptersWrapper.Descendants("div").Any(descendant => descendant.HasClass("volume-element")))
        {
            foreach (HtmlNode volNode in document.DocumentNode.SelectNodes("//div[contains(concat(' ',normalize-space(@class),' '),'volume-element')]"))
            {
                string volume = Regex.Match(volNode.SelectNodes("div").First(node => node.HasClass("volume")).SelectSingleNode("p").InnerText,
                    @"[Vv]olume ([0-9]+).*").Groups[1].Value;
                foreach (HtmlNode chNode in volNode.SelectNodes("div").First(node => node.HasClass("volume-chapters")).SelectNodes("div"))
                {

                    string number = Regex.Match(chNode.SelectSingleNode("a").SelectSingleNode("span").InnerText,
                        @"[Cc]apitolo ([0-9]+).*").Groups[1].Value;
                    string url = chNode.SelectSingleNode("a").GetAttributeValue("href", "");
                    ret.Add(new Chapter(manga, null, volume, number, url));
                }
            }
        }
        else
        {
            foreach (HtmlNode chNode in chaptersWrapper.SelectNodes("div").Where(node => node.HasClass("chapter")))
            {
                string number = chNode.SelectSingleNode("a").SelectSingleNode("span").InnerText.Split(" ")[^1];
                string url = chNode.SelectSingleNode("a").GetAttributeValue("href", "");
                ret.Add(new Chapter(manga, null, null, number, url));
            }
        }

        ret.Reverse();
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
        string requestUrl = $"{chapter.url}?style=list";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            progressToken?.Cancel();
            return requestResult.statusCode;
        }

        if (requestResult.htmlDocument is null)
        {
            progressToken?.Cancel();
            return HttpStatusCode.InternalServerError;
        }

        string[] imageUrls = ParseImageUrlsFromHtml(requestResult.htmlDocument);
        
        string comicInfoPath = Path.GetTempFileName();
        File.WriteAllText(comicInfoPath, chapter.GetComicInfoXmlString());
        
        return DownloadChapterImages(imageUrls, chapter.GetArchiveFilePath(settings.downloadLocation), RequestType.MangaImage, comicInfoPath, "https://www.mangaworld.bz/", progressToken:progressToken);
    }

    private string[] ParseImageUrlsFromHtml(HtmlDocument document)
    {
        List<string> ret = new();

        HtmlNode imageContainer =
            document.DocumentNode.SelectSingleNode("//div[@id='page']");
        foreach(HtmlNode imageNode in imageContainer.Descendants("img"))
            ret.Add(imageNode.GetAttributeValue("src", ""));

        return ret.ToArray();
    }
}