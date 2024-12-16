using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using API.MangaDownloadClients;
using HtmlAgilityPack;

namespace API.Schema.MangaConnectors;

public class Manganato : MangaConnector
{
    public Manganato() : base("Manganato", ["en"], ["manganato.com"])
    {
        this.downloadClient = new HttpDownloadClient();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] GetManga(string publicationTitle = "")
    {
        string sanitizedTitle = string.Join('_', Regex.Matches(publicationTitle, "[A-z]*").Where(str => str.Length > 0)).ToLower();
        string requestUrl = $"https://manganato.com/search/story/{sanitizedTitle}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 ||requestResult.htmlDocument is null)
            return [];
        (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] publications = ParsePublicationsFromHtml(requestResult.htmlDocument);
        return publications;
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] ParsePublicationsFromHtml(HtmlDocument document)
    {
        List<HtmlNode> searchResults = document.DocumentNode.Descendants("div").Where(n => n.HasClass("search-story-item")).ToList();
        List<string> urls = new();
        foreach (HtmlNode mangaResult in searchResults)
        {
            urls.Add(mangaResult.Descendants("a").First(n => n.HasClass("item-title")).GetAttributes()
                .First(a => a.Name == "href").Value);
        }

        List<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)> ret = new();
        foreach (string url in urls)
        {
            (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? manga = GetMangaFromUrl(url);
            if (manga is { } x)
                ret.Add(x);
        }

        return ret.ToArray();
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromId(string publicationId)
    {
        return GetMangaFromUrl($"https://chapmanganato.com/{publicationId}");
    }

    public override (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)? GetMangaFromUrl(string url)
    {
        RequestResult requestResult =
            downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return null;
        
        if (requestResult.htmlDocument is null)
            return null;
        return ParseSinglePublicationFromHtml(requestResult.htmlDocument, url.Split('/')[^1], url);
    }

    private (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?) ParseSinglePublicationFromHtml(HtmlDocument document, string publicationId, string websiteUrl)
    {
        Dictionary<string, string> altTitlesDict = new();
        Dictionary<string, string>? links = null;
        HashSet<string> tags = new();
        string[] authorNames = [];
        string originalLanguage = "";
        MangaReleaseStatus releaseStatus = MangaReleaseStatus.Unreleased;

        HtmlNode infoNode = document.DocumentNode.Descendants("div").First(d => d.HasClass("story-info-right"));

        string sortName = infoNode.Descendants("h1").First().InnerText;

        HtmlNode infoTable = infoNode.Descendants().First(d => d.Name == "table");
        
        foreach (HtmlNode row in infoTable.Descendants("tr"))
        {
            string key = row.SelectNodes("td").First().InnerText.ToLower();
            string value = row.SelectNodes("td").Last().InnerText;
            string keySanitized = string.Concat(Regex.Matches(key, "[a-z]"));

            switch (keySanitized)
            {
                case "alternative":
                    string[] alts = value.Split(" ; ");
                    for(int i = 0; i < alts.Length; i++)
                        altTitlesDict.Add(i.ToString(), alts[i]);
                    break;
                case "authors":
                    authorNames = value.Split('-');
                    for (int i = 0; i < authorNames.Length; i++)
                        authorNames[i] = authorNames[i].Replace("\r\n", "");
                    break;
                case "status":
                    switch (value.ToLower())
                    {
                        case "ongoing": releaseStatus = MangaReleaseStatus.Continuing; break;
                        case "completed": releaseStatus = MangaReleaseStatus.Completed; break;
                    }
                    break;
                case "genres":
                    string[] genres = value.Split(" - ");
                    for (int i = 0; i < genres.Length; i++)
                        genres[i] = genres[i].Replace("\r\n", "");
                    tags = genres.ToHashSet();
                    break;
            }
        }
        List<Author> authors = authorNames.Select(n => new Author(n)).ToList();
        List<MangaTag> mangaTags = tags.Select(n => new MangaTag(n)).ToList();
        List<MangaAltTitle> mangaAltTitles = altTitlesDict.Select(a => new MangaAltTitle(a.Key, a.Value)).ToList();

        string coverUrl = document.DocumentNode.Descendants("span").First(s => s.HasClass("info-image")).Descendants("img").First()
            .GetAttributes().First(a => a.Name == "src").Value;

        string description = document.DocumentNode.Descendants("div").First(d => d.HasClass("panel-story-info-description"))
            .InnerText.Replace("Description :", "");
        while (description.StartsWith('\n'))
            description = description.Substring(1);
        
        string pattern = "MMM dd,yyyy HH:mm";

        HtmlNode? oldestChapter = document.DocumentNode
            .SelectNodes("//span[contains(concat(' ',normalize-space(@class),' '),' chapter-time ')]").MaxBy(
                node => DateTime.ParseExact(node.GetAttributeValue("title", "Dec 31 2400, 23:59"), pattern,
                    CultureInfo.InvariantCulture).Millisecond);


        uint year = (uint)DateTime.ParseExact(oldestChapter?.GetAttributeValue("title", "Dec 31 2400, 23:59")??"Dec 31 2400, 23:59", pattern,
            CultureInfo.InvariantCulture).Year;
        
        Manga manga = new (publicationId, sortName, description, websiteUrl, coverUrl, null, year,
            originalLanguage, releaseStatus, -1, null, null,
            this, 
            authors, 
            mangaTags, 
            [],
            mangaAltTitles);
		
        return (manga, authors, mangaTags, [], mangaAltTitles);
    }

    public override Chapter[] GetChapters(Manga manga, string language="en")
    {
        string requestUrl = $"https://chapmanganato.com/{manga.MangaId}";
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300)
            return [];
        
        //Return Chapters ordered by Chapter-Number
        if (requestResult.htmlDocument is null)
            return [];
        List<Chapter> chapters = ParseChaptersFromHtml(manga, requestResult.htmlDocument);
        return chapters.Order().ToArray();
    }


    private List<Chapter> ParseChaptersFromHtml(Manga manga, HtmlDocument document)
    {
        List<Chapter> ret = new();

        HtmlNode chapterList = document.DocumentNode.Descendants("ul").First(l => l.HasClass("row-content-chapter"));

        Regex volRex = new(@"Vol\.([0-9]+).*");
        Regex chapterRex = new(@"https:\/\/chapmanganato.[A-z]+\/manga-[A-z0-9]+\/chapter-([0-9\.]+)");
        Regex nameRex = new(@"Chapter ([0-9]+(\.[0-9]+)*){1}:? (.*)");

        foreach (HtmlNode chapterInfo in chapterList.Descendants("li"))
        {
            string fullString = chapterInfo.Descendants("a").First(d => d.HasClass("chapter-name")).InnerText;

            string url = chapterInfo.Descendants("a").First(d => d.HasClass("chapter-name"))
                .GetAttributeValue("href", "");
            
            float? volumeNumber = volRex.IsMatch(fullString) ? float.Parse(volRex.Match(fullString).Groups[1].Value) : null;
            float chapterNumber = float.Parse(chapterRex.Match(url).Groups[1].Value);
            string chapterName = nameRex.Match(fullString).Groups[3].Value;
            try
            {
                ret.Add(new Chapter(manga, url, chapterNumber, volumeNumber, chapterName));
            }
            catch (Exception e)
            {
            }
        }
        ret.Reverse();
        return ret;
    }
    
    internal override string[] GetChapterImageUrls(Chapter chapter)
    {
        string requestUrl = chapter.Url;
        RequestResult requestResult =
            downloadClient.MakeRequest(requestUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 ||
            requestResult.htmlDocument is null)
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
            document.DocumentNode.Descendants("div").First(i => i.HasClass("container-chapter-reader"));
        foreach(HtmlNode imageNode in imageContainer.Descendants("img"))
            ret.Add(imageNode.GetAttributeValue("src", ""));

        return ret.ToArray();
    }
}