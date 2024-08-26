using System.Drawing;
using System.Drawing.Imaging;
using System.Net;
using System.Text.RegularExpressions;
using Tranga.Jobs;
using Tranga.MangaConnectors;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2Mangas(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, GetAllCachedManga().Select(m => m.internalId));
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2Manga(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if(!requestParameters.TryGetValue("mangaIds", out string? mangaIdListStr))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.BadRequest, "Missing parameter 'mangaIds'.");
        string[] mangaIdList = mangaIdListStr.Split(',');
        List<Manga> ret = new();
        foreach (string mangaId in mangaIdList)
        {
            if(!_parent.TryGetPublicationById(mangaId, out Manga? manga) || manga is null)
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"Manga with id '{mangaId}' not found.");
            ret.Add(manga.Value);
        }

        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, ret);
    }

    private ValueTuple<HttpStatusCode, object?> GetV2MangaSearch(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if(!requestParameters.TryGetValue("title", out string? title))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.BadRequest, "Missing parameter 'title'.");
        List<Manga> ret = new();
        List<Thread> threads = new();
        foreach (MangaConnector mangaConnector in _connectors)
        {
            Thread t = new (() =>
            {
                ret.AddRange(mangaConnector.GetManga(title));
            });
            t.Start();
            threads.Add(t);
        }
        while(threads.Any(t => t.ThreadState is ThreadState.Running or ThreadState.WaitSleepJoin))
            Thread.Sleep(10);

        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, ret);
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2MangaInternalId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if(groups.Count < 1 ||
           !_parent.TryGetPublicationById(groups[1].Value, out Manga? manga) ||
           manga is null)
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"Manga with ID '{groups[1].Value} could not be found.'");
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, manga);
    }
    
    private ValueTuple<HttpStatusCode, object?> DeleteV2MangaInternalId(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if(groups.Count < 1 ||
           !_parent.TryGetPublicationById(groups[1].Value, out Manga? manga) ||
           manga is null)
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"Manga with ID '{groups[1].Value} could not be found.'");
        Job[] jobs = _parent.jobBoss.GetJobsLike(publication: manga).ToArray();
        _parent.jobBoss.RemoveJobs(jobs);
        RemoveMangaFromCache(groups[1].Value);
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2MangaInternalIdCover(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if(groups.Count < 1 ||
           !_parent.TryGetPublicationById(groups[1].Value, out Manga? manga) ||
           manga is null)
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"Manga with ID '{groups[1].Value} could not be found.'");
        string filePath = manga.Value.coverFileNameInCache!;
        if(!File.Exists(filePath))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, "Cover-File not found.");

        Bitmap bitmap;
        if (requestParameters.TryGetValue("dimensions", out string? dimensionsStr))
        {
            Regex dimensionsRex = new(@"([0-9]+)x([0-9]+)");
            if(!dimensionsRex.IsMatch(dimensionsStr))
                return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.BadRequest, "Requested dimensions not in required format.");
            Match m = dimensionsRex.Match(dimensionsStr);
            int width = int.Parse(m.Groups[1].Value);
            int height = int.Parse(m.Groups[2].Value);
            double aspectRequested = (double)width / (double)height;
            
            using Image coverImage = Image.FromFile(filePath);
            double aspectCover = (double)coverImage.Width / (double)coverImage.Height;

            Size newSize = aspectRequested > aspectCover
                ? new Size(width, (width / coverImage.Width) * coverImage.Height)
                : new Size((height / coverImage.Height) * coverImage.Width, height);

            bitmap = new(coverImage, newSize);
        }
        else
        {
            FileStream coverStream = new(filePath, FileMode.Open);
            bitmap = new(coverStream);
        }
        using MemoryStream ret = new();
        bitmap.Save(ret, ImageFormat.Jpeg);
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, ret);
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2MangaInternalIdChapters(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if(groups.Count < 1 ||
           !_parent.TryGetPublicationById(groups[1].Value, out Manga? manga) ||
           manga is null)
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"Manga with ID '{groups[1].Value} could not be found.'");

        Chapter[] chapters = requestParameters.TryGetValue("language", out string? parameter) switch
        {
            true => manga.Value.mangaConnector.GetChapters((Manga)manga, parameter),
            false => manga.Value.mangaConnector.GetChapters((Manga)manga)
        };
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, chapters);
    }
    
    private ValueTuple<HttpStatusCode, object?> GetV2MangaInternalIdChaptersLatest(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if(groups.Count < 1 ||
           !_parent.TryGetPublicationById(groups[1].Value, out Manga? manga) ||
           manga is null)
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"Manga with ID '{groups[1].Value} could not be found.'");

        float latest = requestParameters.TryGetValue("language", out string? parameter) switch
        {
            true => float.Parse(manga.Value.mangaConnector.GetChapters(manga.Value, parameter).Max().chapterNumber),
            false => float.Parse(manga.Value.mangaConnector.GetChapters(manga.Value).Max().chapterNumber)
        };
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, latest);
    }

    private ValueTuple<HttpStatusCode, object?> PostV2MangaInternalIdIgnoreChaptersBelow(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        if(groups.Count < 1 ||
           !_parent.TryGetPublicationById(groups[1].Value, out Manga? manga) ||
           manga is null)
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"Manga with ID '{groups[1].Value} could not be found.'");
        if (requestParameters.TryGetValue("startChapter", out string? startChapterStr) &&
            float.TryParse(startChapterStr, out float startChapter))
        {
            Manga manga1 = manga.Value;
            manga1.ignoreChaptersBelow = startChapter;
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
        }else
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.InternalServerError, "Parameter 'startChapter' missing, or failed to parse.");
    }

    private ValueTuple<HttpStatusCode, object?> PostV2MangaInternalIdMoveFolder(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        
        if(groups.Count < 1 ||
           !_parent.TryGetPublicationById(groups[1].Value, out Manga? manga) ||
           manga is null)
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, $"Manga with ID '{groups[1].Value} could not be found.'");
        if(!requestParameters.TryGetValue("location", out string? newFolder))
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.BadRequest, "Parameter 'location' missing.");
        manga.Value.MovePublicationFolder(TrangaSettings.downloadLocation, newFolder);
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, null);
    }
}