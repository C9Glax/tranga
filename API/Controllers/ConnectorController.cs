using API.Schema;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Soenneker.Utils.String.NeedlemanWunsch;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{v:apiVersion}/[controller]")]
public class ConnectorController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Get all available Connectors (Scanlation-Sites)
    /// </summary>
    /// <returns>Array of MangaConnector</returns>
    [HttpGet]
    [ProducesResponseType<MangaConnector[]>(Status200OK)]
    public IActionResult GetConnectors()
    {
        MangaConnector[] connectors = context.MangaConnectors.ToArray();
        return Ok(connectors);
    }
    
    /// <summary>
    /// Initiate a search for a Manga on all Connectors
    /// </summary>
    /// <param name="name">Name/Title of the Manga</param>
    /// <returns>Array of Manga</returns>
    [HttpPost("SearchManga")]
    [ProducesResponseType<Manga[]>(Status500InternalServerError)]
    public IActionResult SearchMangaGlobal(string name)
    {
        List<(Manga, Author[], MangaTag[], Link[], MangaAltTitle[])> allManga = new();
        foreach (MangaConnector contextMangaConnector in context.MangaConnectors)
            allManga.AddRange(contextMangaConnector.GetManga(name));
        foreach ((Manga? manga, Author[]? authors, MangaTag[]? tags, Link[]? links, MangaAltTitle[]? altTitles) in allManga)
        {
            try
            {
                AddMangaToContext(manga, authors, tags, links, altTitles);
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new ProblemResponse("An error occurred while processing your request."));
            }
        }
        return Ok(allManga.Select(m => context.Manga.Find(m.Item1.MangaId)).ToArray());
    }
    
    /// <summary>
    /// Initiate a search for a Manga on a specific Connector
    /// </summary>
    /// <param name="id">Manga-Connector-ID</param>
    /// <param name="name">Name/Title of the Manga</param>
    /// <returns>Manga</returns>
    [HttpPost("{id}/SearchManga")]
    [ProducesResponseType<Manga[]>(Status200OK)]
    [ProducesResponseType<ProblemResponse>(Status404NotFound)]
    [ProducesResponseType<ProblemResponse>(Status500InternalServerError)]
    public IActionResult SearchManga(string id, [FromBody]string name)
    {
        MangaConnector? connector = context.MangaConnectors.Find(id);
        if (connector is null)
            return NotFound(new ProblemResponse("Connector not found."));
        (Manga, Author[], MangaTag[], Link[], MangaAltTitle[])[] mangas = connector.GetManga(name);
        foreach ((Manga? manga, Author[]? authors, MangaTag[]? tags, Link[]? links, MangaAltTitle[]? altTitles) in mangas)
        {
            try
            {
                AddMangaToContext(manga, authors, tags, links, altTitles);
            }
            catch (DbUpdateException)
            {
                return StatusCode(500, new ProblemResponse("An error occurred while processing your request."));
            }
        }

        return Ok(mangas.Select(m => context.Manga.Find(m.Item1.MangaId)).ToArray());
    }

    private void AddMangaToContext(Manga? manga, Author[]? authors, MangaTag[]? tags, Link[]? links,
        MangaAltTitle[]? altTitles)
    {
        if (manga is null)
            return;

        if (tags is not null)
        {
            IEnumerable<MangaTag> newTags = tags.Where(mt => context.Tags.All(t => !t.Tag.Equals(mt.Tag)));
            context.Tags.AddRange(newTags);
        }

        if (authors is not null)
        {
            IEnumerable<Author> mergedAuthors = authors.Select(ma =>
            {
                Author? inDb = context.Authors.FirstOrDefault(a => a.Equals(ma));
                return inDb ?? ma;
            });
            manga.Authors = mergedAuthors.ToArray();
            IEnumerable<Author> newAuthors = authors.Where(ma => context.Authors.All(a => !a.Equals(ma)));
            context.Authors.AddRange(newAuthors);
        }

        if (links is not null)
            context.Link.AddRange(links);
        
        if(altTitles is not null)
            context.AltTitles.AddRange(altTitles);
        
        context.Manga.Add(manga);
        
        context.SaveChanges();
    }
}