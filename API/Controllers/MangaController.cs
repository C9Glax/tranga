using API.Controllers.DTOs;
using API.Schema.MangaContext;
using API.Workers;
using API.Workers.MangaDownloadWorkers;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;
using Soenneker.Utils.String.NeedlemanWunsch;
using static Microsoft.AspNetCore.Http.StatusCodes;
using AltTitle = API.Controllers.DTOs.AltTitle;
using Author = API.Controllers.DTOs.Author;
using Link = API.Controllers.DTOs.Link;
using Manga = API.Controllers.DTOs.Manga;

// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class MangaController(MangaContext context) : Controller
{
    
    /// <summary>
    /// Returns all cached <see cref="DTOs.Manga"/>
    /// </summary>
    /// <response code="200"><see cref="MinimalManga"/> exert of <see cref="Schema.MangaContext.Manga"/>. Use <see cref="GetManga"/> for more information</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet]
    [ProducesResponseType<List<MinimalManga>>(Status200OK, "application/json")]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<List<MinimalManga>>, InternalServerError>> GetAllManga ()
    {
        if (await context.Mangas.Include(m => m.MangaConnectorIds)
                .OrderBy(m => m.Name)
                .ToArrayAsync(HttpContext.RequestAborted) is not
            { } result)
            return TypedResults.InternalServerError();
        
        return TypedResults.Ok(result.Select(m =>
        {
            IEnumerable<MangaConnectorId> ids = m.MangaConnectorIds.Select(id => new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
            return new MinimalManga(m.Key, m.Name, m.Description, m.ReleaseStatus, ids);
        }).ToList());
    }
    
    /// <summary>
    /// Returns all <see cref="Schema.MangaContext.Manga"/> that are being downloaded from at least one <see cref="API.MangaConnectors.MangaConnector"/>
    /// </summary>
    /// <response code="200"><see cref="MinimalManga"/> exert of <see cref="Schema.MangaContext.Manga"/>. Use <see cref="GetManga"/> for more information</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet("Downloading")]
    [ProducesResponseType<MinimalManga[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<List<MinimalManga>>, InternalServerError>> GetMangaDownloading()
    {
        if (await context.Mangas
                .Include(m => m.MangaConnectorIds)
                .Where(m => m.MangaConnectorIds.Any(id => id.UseForDownload))
                .OrderBy(m => m.Name)
                .ToArrayAsync(HttpContext.RequestAborted) is not { } result)
            return TypedResults.InternalServerError();

        return TypedResults.Ok(result.Select(m =>
        {
            IEnumerable<MangaConnectorId> ids = m.MangaConnectorIds.Select(id => new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
            return new MinimalManga(m.Key, m.Name, m.Description, m.ReleaseStatus, ids);
        }).ToList());
    }

    /// <summary>
    /// Return <see cref="Schema.MangaContext.Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Schema.MangaContext.Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    [HttpGet("{MangaId}")]
    [ProducesResponseType<Manga>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok<Manga>, NotFound<string>>> GetManga (string MangaId)
    {
        if (await context.MangaIncludeAll().FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return TypedResults.NotFound(nameof(MangaId));
        
        IEnumerable<MangaConnectorId> ids = manga.MangaConnectorIds.Select(id => new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
        IEnumerable<Author> authors = manga.Authors.Select(a => new Author(a.Key, a.AuthorName));
        IEnumerable<string> tags = manga.MangaTags.Select(t => t.Tag);
        IEnumerable<Link> links = manga.Links.Select(l => new Link(l.Key, l.LinkProvider, l.LinkUrl));
        IEnumerable<AltTitle> altTitles = manga.AltTitles.Select(a => new AltTitle(a.Language, a.Title));
        Manga result = new (manga.Key, manga.Name, manga.Description, manga.ReleaseStatus, ids, manga.IgnoreChaptersBefore, manga.Year, manga.OriginalLanguage, manga.ChapterIds, authors, tags, links, altTitles, manga.LibraryId);
        
        return TypedResults.Ok(result);
    }

    /// <summary>
    /// Delete <see cref="Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{MangaId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok, NotFound<string>, InternalServerError<string>>> DeleteManga (string MangaId)
    {
        if(await context.Mangas.Where(m => m.Key == MangaId).ExecuteDeleteAsync(HttpContext.RequestAborted) < 1)
            return TypedResults.NotFound(nameof(MangaId));
        
        if(await context.Sync(HttpContext.RequestAborted, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Ok();
    }


    /// <summary>
    /// Merge two <see cref="Manga"/> into one. THIS IS NOT REVERSIBLE!
    /// </summary>
    /// <param name="MangaIdFrom"><see cref="Manga"/>.Key of <see cref="Manga"/> merging data from (getting deleted)</param>
    /// <param name="MangaIdInto"><see cref="Manga"/>.Key of <see cref="Manga"/> merging data into</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaIdFrom"/> or <paramref name="MangaIdInto"/> not found</response>
    [HttpPost("{MangaIdFrom}/MergeInto/{MangaIdInto}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok, NotFound<string>>> MergeIntoManga (string MangaIdFrom, string MangaIdInto)
    {
        if (await context.MangaIncludeAll().FirstOrDefaultAsync(m => m.Key == MangaIdFrom, HttpContext.RequestAborted) is not { } from)
            return TypedResults.NotFound(nameof(MangaIdFrom));
        if (await context.MangaIncludeAll().FirstOrDefaultAsync(m => m.Key == MangaIdInto, HttpContext.RequestAborted) is not { } into)
            return TypedResults.NotFound(nameof(MangaIdInto));
        
        BaseWorker[] newJobs = into.MergeFrom(from, context);
        Tranga.AddWorkers(newJobs);
        
        return TypedResults.Ok();
    }

    /// <summary>
    /// Returns Cover of <see cref="Manga"/> with <paramref name="MangaId"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <param name="CoverSize">Size of the cover returned
    /// <br /> - <see cref="CoverSize.Small"/> <see cref="Constants.ImageSmSize"/>
    /// <br /> - <see cref="CoverSize.Medium"/> <see cref="Constants.ImageMdSize"/>
    /// <br /> - <see cref="CoverSize.Large"/> <see cref="Constants.ImageLgSize"/>
    /// </param>
    /// <response code="200">JPEG Image</response>
    /// <response code="204">Cover not loaded</response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    /// <response code="503">Retry later, downloading cover</response>
    [HttpGet("{MangaId}/Cover/{CoverSize?}")]
    [ProducesResponseType<FileContentResult>(Status200OK,"image/jpeg")]
    [ProducesResponseType(Status204NoContent)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType(Status503ServiceUnavailable)]
    public async Task<Results<FileContentHttpResult, NoContent, BadRequest, NotFound<string>, StatusCodeHttpResult>> GetCover (string MangaId, CoverSize? CoverSize = null)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return TypedResults.NotFound(nameof(MangaId));

        string cache = CoverSize switch
        {
            MangaController.CoverSize.Small => TrangaSettings.CoverImageCacheSmall,
            MangaController.CoverSize.Medium => TrangaSettings.CoverImageCacheMedium,
            MangaController.CoverSize.Large => TrangaSettings.CoverImageCacheLarge,
            _ => TrangaSettings.CoverImageCacheOriginal
        };

        if (await manga.GetCoverImage(cache, HttpContext.RequestAborted) is not { } data)
        {
            if (Tranga.GetRunningWorkers().Any(worker => worker is DownloadCoverFromMangaconnectorWorker w && context.MangaConnectorToManga.Find(w.MangaConnectorIdId)?.ObjId == MangaId))
            {
                Response.Headers.Append("Retry-After","2");
                return TypedResults.StatusCode(Status503ServiceUnavailable);
            }
            return TypedResults.NoContent();
        }
        
        DateTime lastModified = data.fileInfo.LastWriteTime;
        EntityTagHeaderValue entityTagHeaderValue = EntityTagHeaderValue.Parse($"\"{lastModified.Ticks}\"");
        if(HttpContext.Request.Headers.ETag.Equals(entityTagHeaderValue.Tag.Value))
            return TypedResults.StatusCode(Status304NotModified);
        HttpContext.Response.Headers.CacheControl = "public";
        return TypedResults.Bytes(data.stream.ToArray(), "image/jpeg", lastModified: new DateTimeOffset(lastModified), entityTag: entityTagHeaderValue);
    }
    public enum CoverSize { Original, Large, Medium, Small }

    /// <summary>
    /// Move <see cref="Manga"/> to different <see cref="DTOs.FileLibrary"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <param name="LibraryId"><see cref="DTOs.FileLibrary"/>.Key</param>
    /// <response code="202">Folder is going to be moved</response>
    /// <response code="404"><paramref name="MangaId"/> or <paramref name="LibraryId"/> not found</response>
    [HttpPost("{MangaId}/ChangeLibrary/{LibraryId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok, NotFound<string>>> ChangeLibrary(string MangaId, string LibraryId)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return TypedResults.NotFound(nameof(MangaId));
        if (await context.FileLibraries.FirstOrDefaultAsync(l => l.Key == LibraryId, HttpContext.RequestAborted) is not { } library)
            return TypedResults.NotFound(nameof(LibraryId));
        
        if(manga.LibraryId == library.Key)
            return TypedResults.Ok();

        MoveMangaLibraryWorker moveLibrary = new(manga, library);
        
        Tranga.AddWorkers([moveLibrary]);
        
        return TypedResults.Ok();
    }

    /// <summary>
    /// (Un-)Marks <see cref="Manga"/> as requested for Download from <see cref="API.MangaConnectors.MangaConnector"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/> with <paramref name="MangaId"/></param>
    /// <param name="MangaConnectorName"><see cref="API.MangaConnectors.MangaConnector"/> with <paramref name="MangaConnectorName"/></param>
    /// <param name="IsRequested">true to mark as requested, false to mark as not-requested</param>
    /// <response code="200"></response>
    /// <response code="404"><paramref name="MangaId"/> or <paramref name="MangaConnectorName"/> not found</response>
    /// <response code="412"><see cref="Manga"/> was not linked to <see cref="API.MangaConnectors.MangaConnector"/>, so nothing changed</response>
    /// <response code="428"><see cref="Manga"/> is not linked to <see cref="API.MangaConnectors.MangaConnector"/> yet. Search for <see cref="Manga"/> on <see cref="API.MangaConnectors.MangaConnector"/> first (to create a <see cref="MangaConnectorId{T}"/>).</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("{MangaId}/DownloadFrom/{MangaConnectorName}/{IsRequested}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound,  "text/plain")]
    [ProducesResponseType<string>(Status412PreconditionFailed,  "text/plain")]
    [ProducesResponseType<string>(Status428PreconditionRequired,  "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError,  "text/plain")]
    public async Task<Results<Ok, NotFound<string>, StatusCodeHttpResult, InternalServerError<string>>> MarkAsRequested(string MangaId, string MangaConnectorName, bool IsRequested)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } _)
            return TypedResults.NotFound(nameof(MangaId));
        if(!Tranga.TryGetMangaConnector(MangaConnectorName, out API.MangaConnectors.MangaConnector? _))
            return TypedResults.NotFound(nameof(MangaConnectorName));

        if (context.MangaConnectorToManga
                .FirstOrDefault(id => id.MangaConnectorName == MangaConnectorName && id.ObjId == MangaId)
            is not { } mcId)
        {
            if(IsRequested)
                return TypedResults.StatusCode(Status428PreconditionRequired);
            else
                return TypedResults.StatusCode(Status412PreconditionFailed);
        }

        mcId.UseForDownload = IsRequested;
        if(await context.Sync(HttpContext.RequestAborted, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);

        DownloadCoverFromMangaconnectorWorker downloadCover = new(mcId);
        RetrieveMangaChaptersFromMangaconnectorWorker retrieveChapters = new(mcId, Tranga.Settings.DownloadLanguage);
        Tranga.AddWorkers([downloadCover, retrieveChapters]);
        
        return TypedResults.Ok();
    }
    
    /// <summary>
    /// Initiate a search for <see cref="API.Schema.MangaContext.Manga"/> on a different <see cref="API.MangaConnectors.MangaConnector"/>
    /// </summary>
    /// <param name="MangaId"><see cref="API.Schema.MangaContext.Manga"/> with <paramref name="MangaId"/></param>
    /// <param name="MangaConnectorName"><see cref="API.MangaConnectors.MangaConnector"/>.Name</param>
    /// <response code="200"><see cref="MinimalManga"/> exert of <see cref="Schema.MangaContext.Manga"/></response>
    /// <response code="404"><see cref="API.MangaConnectors.MangaConnector"/> with Name not found</response>
    /// <response code="412"><see cref="API.MangaConnectors.MangaConnector"/> with Name is disabled</response>
    [HttpGet("{MangaId}/OnMangaConnector/{MangaConnectorName}")]
    [ProducesResponseType<List<MinimalManga>>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType(Status406NotAcceptable)]
    public async Task<Results<Ok<List<MinimalManga>>, NotFound<string>, StatusCodeHttpResult>> SearchOnDifferentConnector (string MangaId, string MangaConnectorName)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return TypedResults.NotFound(nameof(MangaId));

        return new SearchController(context).SearchManga(MangaConnectorName, manga.Name);
    }
    
    /// <summary>
    /// Returns all <see cref="Manga"/> which where Authored by <see cref="Author"/> with <paramref name="AuthorId"/>
    /// </summary>
    /// <param name="AuthorId"><see cref="Author"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Author"/> with <paramref name="AuthorId"/></response>
    /// /// <response code="500">Error during Database Operation</response>
    [HttpGet("WithAuthorId/{AuthorId}")]
    [ProducesResponseType<List<Manga>>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok<List<Manga>>, NotFound<string>, InternalServerError>> GetMangaWithAuthorIds (string AuthorId)
    {
        if (await context.Authors.FirstOrDefaultAsync(a => a.Key == AuthorId, HttpContext.RequestAborted) is not { } _)
            return TypedResults.NotFound(nameof(AuthorId));

        if (await context.MangaIncludeAll()
                .Where(m => m.Authors.Any(a => a.Key == AuthorId))
                .OrderBy(m => m.Name)
                .ToListAsync(HttpContext.RequestAborted) is not { } result)
            return TypedResults.InternalServerError();

        return TypedResults.Ok(result.Select(m =>
        {
            IEnumerable<MangaConnectorId> ids = m.MangaConnectorIds.Select(id => new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
            IEnumerable<Author> authors = m.Authors.Select(a => new Author(a.Key, a.AuthorName));
            IEnumerable<string> tags = m.MangaTags.Select(t => t.Tag);
            IEnumerable<Link> links = m.Links.Select(l => new Link(l.Key, l.LinkProvider, l.LinkUrl));
            IEnumerable<AltTitle> altTitles = m.AltTitles.Select(a => new AltTitle(a.Language, a.Title));
            return new Manga(m.Key, m.Name, m.Description, m.ReleaseStatus, ids, m.IgnoreChaptersBefore, m.Year, m.OriginalLanguage, m.ChapterIds, authors, tags, links, altTitles, m.LibraryId);
        }).ToList());
    }
    
    /// <summary>
    /// Returns all <see cref="Manga"/> with <see cref="Tag"/>
    /// </summary>
    /// <param name="Tag"><see cref="Tag"/>.Tag</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Tag"/> not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet("WithTag/{Tag}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<List<MinimalManga>>, NotFound<string>, InternalServerError>> GetMangasWithTag (string Tag)
    {
        if (await context.Mangas
                .Include(m => m.MangaConnectorIds)
                .Include(m => m.MangaTags)
                .Where(m => m.MangaTags.Any(t => t.Tag == Tag))
                .OrderBy(m => m.Name)
                .ToListAsync(HttpContext.RequestAborted) is not { } result)
            return TypedResults.InternalServerError();
        
        return TypedResults.Ok(result.Select(m =>
        {
            IEnumerable<MangaConnectorId> ids = m.MangaConnectorIds.Select(id => new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
            return new MinimalManga(m.Key, m.Name, m.Description, m.ReleaseStatus, ids);
        }).ToList());
    }

    /// <summary>
    /// Returns <see cref="Schema.MangaContext.Manga"/> with names similar to <see cref="Schema.MangaContext.Manga"/> (identified by <paramref name="MangaId"/>)
    /// </summary>
    /// <param name="MangaId">Key of <see cref="Schema.MangaContext.Manga"/></param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="Schema.MangaContext.Manga"/> with <paramref name="MangaId"/> not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet("WithSimilarName/{MangaId}")]
    [ProducesResponseType<List<string>>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<List<string>>, NotFound<string>, InternalServerError>> GetSimilarManga (string MangaId)
    {
        if (await context.Mangas.FirstOrDefaultAsync(m => m.Key == MangaId, HttpContext.RequestAborted) is not { } manga)
            return TypedResults.NotFound(nameof(MangaId));
        
        string name = manga.Name;

        if (await context.Mangas.Where(m => m.Key != MangaId)
                .ToDictionaryAsync(m => m.Key, m => m.Name, HttpContext.RequestAborted) is not { } mangaNames)
            return TypedResults.InternalServerError();

        List<string> similarIds = mangaNames
            .Where(kv => NeedlemanWunschStringUtil.CalculateSimilarityPercentage(name, kv.Value) > 0.8)
            .Select(kv => kv.Key)
            .ToList();
        
        return TypedResults.Ok(similarIds);
    }

    /// <summary>
    /// Returns the <see cref="MangaConnectorId{Manga}"/> with <see cref="MangaConnectorId{Manga}"/>.Key
    /// </summary>
    /// <param name="MangaConnectorIdId">Key of <see cref="MangaConnectorId{Manga}"/></param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="MangaConnectorId{Manga}"/> with <paramref name="MangaConnectorIdId"/> not found</response>
    [HttpGet("ConnectorId/{MangaConnectorIdId}")]
    [ProducesResponseType<MangaConnectorId>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok<MangaConnectorId>, NotFound<string>>> GetMangaMangaConnectorId (string MangaConnectorIdId)
    {
        if (await context.MangaConnectorToManga.FirstOrDefaultAsync(c => c.Key == MangaConnectorIdId, HttpContext.RequestAborted) is not { } mcIdManga)
            return TypedResults.NotFound(nameof(MangaConnectorIdId));

        MangaConnectorId result = new (mcIdManga.Key, mcIdManga.MangaConnectorName, mcIdManga.ObjId, mcIdManga.WebsiteUrl, mcIdManga.UseForDownload);
        
        return TypedResults.Ok(result);
    }
}