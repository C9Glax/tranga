using API.Schema;
using API.Schema.Contexts;
using Asp.Versioning;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soenneker.Utils.String.NeedlemanWunsch;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class SearchController(PgsqlContext context, ILog Log) : Controller
{
    /// <summary>
    /// Initiate a search for a Obj on a specific Connector
    /// </summary>
    /// <param name="MangaConnectorName"></param>
    /// <param name="Query"></param>
    /// <response code="200"></response>
    /// <response code="404">MangaConnector with ID not found</response>
    /// <response code="406">MangaConnector with ID is disabled</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet("{MangaConnectorName}/{Query}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status406NotAcceptable)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult SearchManga(string MangaConnectorName, string Query)
    {
        if(context.MangaConnectors.Find(MangaConnectorName) is not { } connector)
            return NotFound();
        else if (connector.Enabled is false)
            return StatusCode(Status406NotAcceptable);
        
        (Manga, MangaConnectorId<Manga>)[] mangas = connector.SearchManga(Query);
        List<Manga> retMangas = new();
        foreach ((Manga manga, MangaConnectorId<Manga> mcId) manga in mangas)
        {
            try
            {
                if(AddMangaToContext(manga) is { } add)
                    retMangas.Add(add);
            }
            catch (DbUpdateException e)
            {
                Log.Error(e);
                return StatusCode(Status500InternalServerError, e.Message);
            }
        }

        return Ok(retMangas.ToArray());
    }
    
    /// <summary>
    /// Search for a known Obj
    /// </summary>
    /// <param name="Query"></param>
    /// <response code="200"></response>
    [HttpGet("Local/{Query}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    public IActionResult SearchMangaLocally(string Query)
    {
        Dictionary<Manga, double> distance = context.Mangas
            .ToArray()
            .ToDictionary(m => m, m => NeedlemanWunschStringUtil.CalculateSimilarityPercentage(Query, m.Name));
        return Ok(distance.Where(kv => kv.Value > 50).OrderByDescending(kv => kv.Value).Select(kv => kv.Key).ToArray());
    }

    /// <summary>
    /// Returns Obj from MangaConnector associated with URL
    /// </summary>
    /// <param name="url">Obj-Page URL</param>
    /// <response code="200"></response>
    /// <response code="300">Multiple connectors found for URL</response>
    /// <response code="404">Obj not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("Url")]
    [ProducesResponseType<Manga>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult GetMangaFromUrl([FromBody]string url)
    {
        if (context.MangaConnectors.Find("Global") is not { } connector)
            return StatusCode(Status500InternalServerError, "Could not find Global Connector.");

        if(connector.GetMangaFromUrl(url) is not { } manga)
            return NotFound();
        try
        {
            if(AddMangaToContext(manga) is { } add)
                return Ok(add);
            return StatusCode(Status500InternalServerError);
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
            return StatusCode(Status500InternalServerError, e.Message);
        }
    }
    
    private Manga? AddMangaToContext((Manga, MangaConnectorId<Manga>) manga) => AddMangaToContext(manga.Item1, manga.Item2, context);
    
    internal static Manga? AddMangaToContext(Manga addManga, MangaConnectorId<Manga> addMcId, PgsqlContext context)
    {
        Manga manga = context.Mangas.Find(addManga.Key) ?? addManga;
        MangaConnectorId<Manga> mcId = context.MangaConnectorToManga.Find(addMcId.Key) ?? addMcId;
        mcId.Obj = manga;
        
        IEnumerable<MangaTag> mergedTags = manga.MangaTags.Select(mt =>
        {
            MangaTag? inDb = context.Tags.Find(mt.Tag);
            return inDb ?? mt;
        });
        manga.MangaTags = mergedTags.ToList();

        IEnumerable<Author> mergedAuthors = manga.Authors.Select(ma =>
        {
            Author? inDb = context.Authors.Find(ma.Key);
            return inDb ?? ma;
        });
        manga.Authors = mergedAuthors.ToList();
        
        try
        {
            if(context.MangaConnectorToManga.Find(addMcId.Key) is null)
                context.MangaConnectorToManga.Add(mcId);
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            return null;
        }
        return manga;
    }
}