using API.Schema;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using Asp.Versioning;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class SearchController(PgsqlContext context, ILog Log) : Controller
{
    /// <summary>
    /// Initiate a search for a Manga on a specific Connector
    /// </summary>
    /// <param name="MangaConnectorName"></param>
    /// <param name="Query"></param>
    /// <response code="200"></response>
    /// <response code="404">MangaConnector with ID not found</response>
    /// <response code="406">MangaConnector with ID is disabled</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{MangaConnectorName}/{Query}")]
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
        
        Manga[] mangas = connector.SearchManga(Query);
        List<Manga> retMangas = new();
        foreach (Manga manga in mangas)
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
    /// Returns Manga from MangaConnector associated with URL
    /// </summary>
    /// <param name="url">Manga-Page URL</param>
    /// <response code="200"></response>
    /// <response code="300">Multiple connectors found for URL</response>
    /// <response code="400">No Manga at URL</response>
    /// <response code="404">No connector found for URL</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("Url")]
    [ProducesResponseType<Manga>(Status200OK, "application/json")]
    [ProducesResponseType(Status300MultipleChoices)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult GetMangaFromUrl([FromBody]string url)
    {
        List<MangaConnector> connectors = context.MangaConnectors.AsEnumerable().Where(c => c.UrlMatchesConnector(url)).ToList();
        if (connectors.Count == 0)
            return NotFound();
        else if (connectors.Count > 1)
            return StatusCode(Status300MultipleChoices);

        if(connectors.First().GetMangaFromUrl(url) is not { } manga)
            return BadRequest();
        try
        {
            if(AddMangaToContext(manga) is { } add)
                return Ok(add);
            return StatusCode(500);
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }
    
    private Manga? AddMangaToContext(Manga manga)
    {
        Manga? existing = context.Mangas.Find(manga.MangaId);
        
        IEnumerable<MangaTag> mergedTags = manga.MangaTags.Select(mt =>
        {
            MangaTag? inDb = context.Tags.Find(mt.Tag);
            return inDb ?? mt;
        });
        manga.MangaTags = mergedTags.ToList();

        IEnumerable<Author> mergedAuthors = manga.Authors.Select(ma =>
        {
            Author? inDb = context.Authors.Find(ma.AuthorId);
            return inDb ?? ma;
        });
        manga.Authors = mergedAuthors.ToList();

        /*
        IEnumerable<Link> mergedLinks = manga.Links.Select(ml =>
        {
            Link? inDb = context.Links.Find(ml.LinkId);
            return inDb ?? ml;
        });
        manga.Links = mergedLinks.ToList();

        IEnumerable<MangaAltTitle> mergedAltTitles = manga.AltTitles.Select(mat =>
        {
            MangaAltTitle? inDb = context.AltTitles.Find(mat.AltTitleId);
            return inDb ?? mat;
        });
        manga.AltTitles = mergedAltTitles.ToList();
*/
        try
        {

            if (context.Mangas.Find(manga.MangaId) is { } r)
            {
                context.Mangas.Remove(r);
                context.SaveChanges();
            }
            context.Mangas.Add(manga);
            context.Jobs.Add(new DownloadMangaCoverJob(manga));
            context.SaveChanges();
        }
        catch (DbUpdateException e)
        {
            Log.Error(e);
            return null;
        }
        return manga;
    }
}