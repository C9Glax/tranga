using System.Net;
using System.Text.RegularExpressions;
using Tranga.Jobs;

namespace Tranga.Server;

public partial class Server
{
    private ValueTuple<HttpStatusCode, object?> GetV2Manga(GroupCollection groups, Dictionary<string, string> requestParameters)
    {
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, GetAllCachedManga().Select(m => m.internalId));
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
        string filePath = settings.GetFullCoverPath((Manga)manga!);
        if (File.Exists(filePath))
        {
            FileStream coverStream = new(filePath, FileMode.Open);
            return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.OK, coverStream);
        }
        return new ValueTuple<HttpStatusCode, object?>(HttpStatusCode.NotFound, "Cover-File not found.");
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
}