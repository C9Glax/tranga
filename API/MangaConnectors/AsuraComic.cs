using System.Text.RegularExpressions;
using HtmlAgilityPack;
using API.MangaDownloadClients;
using API.Schema.MangaContext;
using Newtonsoft.Json.Linq;

namespace API.MangaConnectors;

public class AsuraComic : MangaConnector
{
    public AsuraComic() : base("AsuraComic", ["en"], ["asuracomic.net"], "https://asuracomic.net/favicon.ico")
    {
        this.downloadClient = new HttpDownloadClient();
    }

    public override (Manga, MangaConnectorId<Manga>)[] SearchManga(string mangaSearchName)
    {
        Log.Info($"Searching Obj: {mangaSearchName}");
        string sanitizedTitle = string.Join(' ', Regex.Matches(mangaSearchName, "[A-z]*").Where(m => m.Value.Length > 0)).ToLower();
        string requestUrl = $"https://asuracomic.net/series?name={sanitizedTitle}";
        RequestResult requestResult = downloadClient.MakeRequest(requestUrl, RequestType.Default);

        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
        {
            Log.Error("Request failed or site retrieval failed");
            return [];
        }

        List<(Manga, MangaConnectorId<Manga>)> mangas = new();
        HtmlNodeCollection? nodes = requestResult.htmlDocument.DocumentNode.SelectNodes("//a[starts-with(@href,'series')]");
        if (nodes is null || nodes.Count < 1)
        {
            Log.Error("No series nodes found");
            return [];
        }

        foreach (HtmlNode node in nodes)
        {
            string href = node.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(href))
            {
                string url = $"https://asuracomic.net/{href}";
                var manga = GetMangaFromUrl(url);
                if (manga is not null)
                    mangas.Add(manga.Value);
            }
        }

        Log.Info($"Search {mangaSearchName} yielded {mangas.Count} results.");
        return mangas.ToArray();
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromUrl(string url)
    {
        Log.Info($"Getting Obj: {url}");
        RequestResult requestResult = downloadClient.MakeRequest(url, RequestType.MangaInfo);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
        {
            Log.Error("Failed to retrieve site");
            return null;
        }

        return ParseMangaFromHtml(requestResult.htmlDocument, url.Split('/')[^1], url);
    }

    public override (Manga, MangaConnectorId<Manga>)? GetMangaFromId(string mangaIdOnSite)
    {
        return GetMangaFromUrl($"https://asuracomic.net/series/{mangaIdOnSite}");
    }

    private (Manga, MangaConnectorId<Manga>) ParseMangaFromHtml(HtmlDocument doc, string mangaId, string url)
    {
        HtmlNode titleNode = doc.DocumentNode.SelectSingleNode("//title");
        string name = Regex.Match(titleNode.InnerText, @"(.*) - Asura Scans").Groups[1].Value;

        HtmlNode? coverNode = doc.DocumentNode.SelectSingleNode("//img[@alt='poster']");
        string coverUrl = coverNode?.GetAttributeValue("src", "") ?? "";

        HtmlNode? descriptionNode = doc.DocumentNode.SelectSingleNode("//h3[starts-with(text(),'Synopsis')]/../span");
        string description = descriptionNode?.InnerText ?? "";

        HtmlNodeCollection genreNodes = doc.DocumentNode.SelectNodes("//h3[text()='Genres']/../div/button");
        List<MangaTag> tags = genreNodes?.Select(b => new MangaTag(b.InnerText)).ToList() ?? [];

        HtmlNode statusNode = doc.DocumentNode.SelectSingleNode("//h3[text()='Status']/../h3[2]");
        MangaReleaseStatus releaseStatus = statusNode?.InnerText.ToLower() switch
        {
            "ongoing" => MangaReleaseStatus.Continuing,
            "hiatus" => MangaReleaseStatus.OnHiatus,
            "completed" => MangaReleaseStatus.Completed,
            "dropped" => MangaReleaseStatus.Cancelled,
            "season end" => MangaReleaseStatus.Continuing,
            _ => MangaReleaseStatus.Unreleased
        };

        HtmlNodeCollection authorNodes = doc.DocumentNode.SelectNodes("//h3[text()='Author']/../h3[not(text()='Author' or text()='_')]");
        HtmlNodeCollection artistNodes = doc.DocumentNode.SelectNodes("//h3[text()='Artist']/../h3[not(text()='Artist' or text()='_')]");
        List<Author> authors = authorNodes?.Select(a => new Author(a.InnerText)).ToList() ?? [];
        if (artistNodes is not null)
            authors.AddRange(artistNodes.Select(a => new Author(a.InnerText)));

        HtmlNode? firstChapterNode = doc.DocumentNode.SelectSingleNode("//a[contains(@href, 'chapter/1')]/../following-sibling::h3");
        uint? year = uint.TryParse(firstChapterNode?.InnerText.Split(' ').LastOrDefault(), out uint parsed) ? parsed : null;

        Manga manga = new(name, description, coverUrl, releaseStatus, authors, tags, [], [], null, 0f, year, null);
        string websiteUrl = $"https://asuracomic.net/series/{mangaId}";
        MangaConnectorId<Manga> mcId = new(manga, this, mangaId, websiteUrl);
        manga.MangaConnectorIds.Add(mcId);
        return (manga, mcId);
    }

    public override (Chapter, MangaConnectorId<Chapter>)[] GetChapters(MangaConnectorId<Manga> manga, string? language = null)
    {
        Log.Info($"Getting Chapters: {manga.IdOnConnectorSite}");
        RequestResult requestResult = downloadClient.MakeRequest(manga.WebsiteUrl, RequestType.Default);
        if ((int)requestResult.statusCode < 200 || (int)requestResult.statusCode >= 300 || requestResult.htmlDocument is null)
        {
            Log.Error("Failed to load site");
            return [];
        }

        HtmlNodeCollection? chapterNodes = requestResult.htmlDocument.DocumentNode.SelectNodes("//a[contains(@href, '/chapter/')]");
        if (chapterNodes is null)
            return [];

        Regex chapterRegex = new(@"Chapter ([0-9]+)(.*)?");
        List<(Chapter, MangaConnectorId<Chapter>)> chapters = new();

        foreach (HtmlNode chapterNode in chapterNodes)
        {
            string href = chapterNode.GetAttributeValue("href", "");
            string url = $"https://asuracomic.net/{href}";
            Match match = chapterRegex.Match(chapterNode.InnerText);
            string chapterNumber = match.Groups[1].Value;
            string? chapterTitle = match.Groups[2].Success ? match.Groups[2].Value.Trim() : null;

            Chapter chapter = new(manga.Obj, chapterNumber, null, chapterTitle);
            MangaConnectorId<Chapter> mcId = new(chapter, this, href, url);
            chapter.MangaConnectorIds.Add(mcId);
            chapters.Add((chapter, mcId));
        }

        Log.Info($"Request for chapters for {manga.Obj.Name} yielded {chapters.Count} results.");
        return chapters.ToArray();
    }

    internal override string[] GetChapterImageUrls(MangaConnectorId<Chapter> chapterId)
    {
        Log.Info($"Getting Chapter Image-Urls: {chapterId.Obj}");
        if (chapterId.WebsiteUrl is null)
        {
            Log.Error("Chapter URL is null");
            return [];
        }

        RequestResult result = downloadClient.MakeRequest(chapterId.WebsiteUrl, RequestType.Default);
        if ((int)result.statusCode < 200 || (int)result.statusCode >= 300 || result.htmlDocument is null)
        {
            Log.Error("Failed to load chapter page");
            return [];
        }

        HtmlNodeCollection? imageNodes = result.htmlDocument.DocumentNode.SelectNodes("//img[contains(@alt, 'chapter page')]");
        if (imageNodes is null)
            return [];

        return imageNodes.Select(i => i.GetAttributeValue("src", "")).ToArray();
    }
}