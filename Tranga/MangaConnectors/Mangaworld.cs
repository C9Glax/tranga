using System.Net;
using System.Text.RegularExpressions;
using API.Schema;
using HtmlAgilityPack;

namespace Tranga.MangaConnectors;

public class Mangaworld : MangaConnector
{
    //["it"], ["www.mangaworld.ac"]
    public Mangaworld(string mangaConnectorId) : base(mangaConnectorId, new HttpDownloadClient())
    {
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] GetManga(string publicationTitle = "")
    {
        log.Info($"Searching Publications. Term=\"{publicationTitle}\"");
        string sanitizedTitle = string.Join(' ', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0)).ToLower();
        string requestUrl = $"https://www.mangaworld.ac/archive?keyword={sanitizedTitle}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return [];

        if (requestResult.htmlDocument is null)
            return [];
        (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        log.Info($"Retrieved {publications.Length} publications. Term=\"{publicationTitle}\"");
        return publications;
    }

    private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        if (!document.DocumentNode.SelectSingleNode("//div[@class='comics-grid']").ChildNodes
                .Any(node => node.HasClass("entry")))
            return [];
        
        List<string> urls = document.DocumentNode
            .SelectNodes(
                "//div[@class='comics-grid']//div[@class='entry']//a[contains(concat(' ',normalize-space(@class),' '),'thumb')]")
            .Select(thumb => thumb.GetAttributeValue("href", "")).ToList();

        List<(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])> ret = new();
        foreach (string url in urls)
        {
            (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? manga = GetMangaFromUrl(url);
            if (manga is not null)
                ret.Add(((Manga, Author[], MangaTag[], Link[], MangaAltTitle[]))manga);
        }

        return ret.ToArray();
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://www.mangaworld.ac/manga/{publicationId}");
    }

    public override (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])? GetMangaFromUrl(string url)
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

    private (Manga, Author[], MangaTag[], Link[], MangaAltTitle[]) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        string originalLanguage = "";
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

        HtmlNode infoNode = document.DocumentNode.Descendants("div").First(d => d.HasClass("info"));

        string sortName = infoNode.Descendants("h1").First().InnerText;

        HtmlNode metadata = infoNode.Descendants().First(d => d.HasClass("meta-data"));

        HtmlNode altTitlesNode = metadata.SelectSingleNode("//span[text()='Titoli alternativi: ' or text()='Titolo alternativo: ']/..").ChildNodes[1];

        int i = 0;
        MangaAltTitle[] alts = altTitlesNode.InnerText.Split(", ").Select(v => new MangaAltTitle(i++.ToString(), v)).ToArray();

        HtmlNode genresNode =
            metadata.SelectSingleNode("//span[text()='Generi: ' or text()='Genero: ']/..");
        MangaTag[] tags = genresNode.SelectNodes("a").Select(node => new MangaTag(node.InnerText)).ToArray();
        
        HtmlNode authorsNode =
            metadata.SelectSingleNode("//span[text()='Autore: ' or text()='Autori: ']/..");
        Author[] authors = authorsNode.SelectNodes("a").Select(node => new Author(node.InnerText)).ToArray();

        string status = metadata.SelectSingleNode("//span[text()='Stato: ']/..").SelectNodes("a").First().InnerText;
        // ReSharper disable 5 times StringLiteralTypo
        switch (status.ToLower())
        {
            case "cancellato": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "in pausa": releaseStatus = MangaReleaseStatus.OnHiatus; break;
            case "droppato": releaseStatus = MangaReleaseStatus.Cancelled; break;
            case "finito": releaseStatus = MangaReleaseStatus.Completed; break;
            case "in corso": releaseStatus = MangaReleaseStatus.Continuing; break;
        }

        string posterUrl = document.DocumentNode.SelectSingleNode("//img[@class='rounded']").GetAttributeValue("src", "");

        string description = document.DocumentNode.SelectSingleNode("//div[@id='noidungm']").InnerText;
        
        string yearString = metadata.SelectSingleNode("//span[text()='Anno di uscita: ']/..").SelectNodes("a").First().InnerText;
        uint year = uint.Parse(yearString);
        
        Manga manga = new(MangaConnectorId, sortName, description, posterUrl, null, year, originalLanguage,
            releaseStatus, 0, null, null, publicationId,
            authors.Select(a => a.AuthorId).ToArray(),
            tags.Select(t => t.Tag).ToArray(),
            [],
            alts.Select(a => a.AltTitleId).ToArray());
        return (manga, authors, tags, [], alts);
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        log.Info($"Getting chapters {manga}");
        string requestUrl = $"https://www.mangaworld.ac/manga/{manga.MangaConnectorId}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return Array.Empty<Chapter>();
        
        //Return Chapters ordered by Chapter-Number
        if (requestResult.htmlDocument is null)
            return Array.Empty<Chapter>();
        List<Chapter> chapters = ParseChaptersFromHtml(manga, requestResult.htmlDocument);
        log.Info($"Got {chapters.Count} chapters. {manga}");
        return chapters.Order().ToArray();
    }

    private List<Chapter> ParseChaptersFromHtml(Manga manga, HtmlDocument document)
    {
        List<Chapter> ret = new();

        HtmlNode chaptersWrapper =
            document.DocumentNode.SelectSingleNode(
                "//div[contains(concat(' ',normalize-space(@class),' '),'chapters-wrapper')]");

        Regex volumeRex = new(@"[Vv]olume ([0-9]+).*");
        Regex chapterRex = new(@"[Cc]apitolo ([0-9]+).*");
        Regex idRex = new(@".*\/read\/([a-z0-9]+)(?:[?\/].*)?");
        if (chaptersWrapper.Descendants("div").Any(descendant => descendant.HasClass("volume-element")))
        {
            foreach (HtmlNode volNode in document.DocumentNode.SelectNodes("//div[contains(concat(' ',normalize-space(@class),' '),'volume-element')]"))
            {
                string volume = volumeRex.Match(volNode.SelectNodes("div").First(node => node.HasClass("volume")).SelectSingleNode("p").InnerText).Groups[1].Value;
                foreach (HtmlNode chNode in volNode.SelectNodes("div").First(node => node.HasClass("volume-chapters")).SelectNodes("div"))
                {
                    string number = chapterRex.Match(chNode.SelectSingleNode("a").SelectSingleNode("span").InnerText).Groups[1].Value;
                    string url = chNode.SelectSingleNode("a").GetAttributeValue("href", "");
                    string id = idRex.Match(chNode.SelectSingleNode("a").GetAttributeValue("href", "")).Groups[1].Value;
                    if (!float.TryParse(volume, NumberFormatDecimalPoint, out float volNum))
                    {
                        log.Debug($"Failed parsing {volume} as float.");
                        continue;
                    }
                    if (!float.TryParse(number, NumberFormatDecimalPoint, out float chNum))
                    {
                        log.Debug($"Failed parsing {number} as float.");
                        continue;
                    }
                    ret.Add(new Chapter(manga, url, chNum, volNum));
                }
            }
        }
        else
        {
            foreach (HtmlNode chNode in chaptersWrapper.SelectNodes("div").Where(node => node.HasClass("chapter")))
            {
                string number = chapterRex.Match(chNode.SelectSingleNode("a").SelectSingleNode("span").InnerText).Groups[1].Value;
                string url = chNode.SelectSingleNode("a").GetAttributeValue("href", "");
                string id = idRex.Match(chNode.SelectSingleNode("a").GetAttributeValue("href", "")).Groups[1].Value;
                if (!float.TryParse(number, NumberFormatDecimalPoint, out float chNum))
                {
                    log.Debug($"Failed parsing {number} as float.");
                    continue;
                }
                ret.Add(new Chapter(manga, url, chNum));
            }
        }

        ret.Reverse();
        return ret;
    }

    protected override string[] GetChapterImages(Chapter chapter)
    {
        Manga chapterParentManga = chapter.ParentManga;
        log.Info($"Retrieving chapter-info {chapter} {chapterParentManga}");
        string requestUrl = $"{chapter.Url}?style=list";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
        {
            return [];
        }

        if (requestResult.htmlDocument is null)
        {
            return [];
        }

        string[] imageUrls = ParseImageUrlsFromHtml(requestResult.htmlDocument);

        return imageUrls;
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