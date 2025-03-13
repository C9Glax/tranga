using API.Schema;
using API.Schema.Jobs;
using API.Schema.MangaConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class SearchController(PgsqlContext context) : Controller
{
    
    /// <summary>
    /// Initiate a search for a Manga on all Connectors
    /// </summary>
    /// <param name="name">Name/Title of the Manga</param>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("Name")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult SearchMangaGlobal([FromBody]string name)
    {
        List<(Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)> allManga = new();
        foreach (MangaConnector contextMangaConnector in context.MangaConnectors.Where(connector => connector.Enabled))
            allManga.AddRange(contextMangaConnector.GetManga(name));
        
        List<Manga> retMangas = new();
        foreach ((Manga? manga, List<Author>? authors, List<MangaTag>? tags, List<Link>? links, List<MangaAltTitle>? altTitles) in allManga)
        {
            try
            {
                Manga? add = AddMangaToContext(manga, authors, tags, links, altTitles);
                if(add is not null)
                    retMangas.Add(add);
            }
            catch (Exception e)
            {
                return StatusCode(500, e);
            }
        }
        return Ok(retMangas.ToArray());
    }
    
    /// <summary>
    /// Initiate a search for a Manga on a specific Connector
    /// </summary>
    /// <param name="MangaConnectorName">Manga-Connector-ID</param>
    /// <param name="name">Name/Title of the Manga</param>
    /// <response code="200"></response>
    /// <response code="404">MangaConnector with ID not found</response>
    /// <response code="406">MangaConnector with ID is disabled</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{MangaConnectorName}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status406NotAcceptable)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult SearchManga(string MangaConnectorName, [FromBody]string name)
    {
        MangaConnector? connector = context.MangaConnectors.Find(MangaConnectorName);
        if (connector is null)
            return NotFound();
        else if (connector.Enabled is false)
            return StatusCode(406);
        
        (Manga, List<Author>?, List<MangaTag>?, List<Link>?, List<MangaAltTitle>?)[] mangas = connector.GetManga(name);
        List<Manga> retMangas = new();
        foreach ((Manga? manga, List<Author>? authors, List<MangaTag>? tags, List<Link>? links, List<MangaAltTitle>? altTitles) in mangas)
        {
            try
            {
                Manga? add = AddMangaToContext(manga, authors, tags, links, altTitles);
                if(add is not null)
                    retMangas.Add(add);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
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
        List<MangaConnector> connectors = context.MangaConnectors.AsEnumerable().Where(c => c.ValidateUrl(url)).ToList();
        if (connectors.Count == 0)
            return NotFound();
        else if (connectors.Count > 1)
            return StatusCode(Status300MultipleChoices);

        (Manga manga, List<Author>? authors, List<MangaTag>? tags, List<Link>? links, List<MangaAltTitle>? altTitles)? x = connectors.First().GetMangaFromUrl(url);
        if (x is null)
            return BadRequest();
        try
        {
            Manga? add = AddMangaToContext(x.Value.manga, x.Value.authors, x.Value.tags, x.Value.links, x.Value.altTitles);
            if (add is not null)
                return Ok(add);
            return StatusCode(500);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
    
    private Manga? AddMangaToContext(Manga? manga, List<Author>? authors, List<MangaTag>? tags, List<Link>? links,
        List<MangaAltTitle>? altTitles)
    {
        if (manga is null)
            return null;
        
        Manga? existing = context.Manga.FirstOrDefault(m =>
            m.MangaId == manga.MangaId);
        
        if (tags is not null)
        {
            IEnumerable<MangaTag> mergedTags = tags.Select(mt =>
            {
                MangaTag? inDb = context.Tags.FirstOrDefault(t => t.Equals(mt));
                return inDb ?? mt;
            });
            manga.MangaTags = mergedTags.ToList();
            IEnumerable<MangaTag> newTags = manga.MangaTags.Where(mt => !context.Tags.Any(t => t.Tag.Equals(mt.Tag)));
            context.Tags.AddRange(newTags);
        }

        if (authors is not null)
        {
            IEnumerable<Author> mergedAuthors = authors.Select(ma =>
            {
                Author? inDb = context.Authors.FirstOrDefault(a => a.AuthorName == ma.AuthorName);
                return inDb ?? ma;
            });
            manga.Authors = mergedAuthors.ToList();
            IEnumerable<Author> newAuthors = manga.Authors.Where(ma => !context.Authors.Any(a =>
                a.AuthorName == ma.AuthorName));
            context.Authors.AddRange(newAuthors);
        }

        if (links is not null)
        {
            IEnumerable<Link> mergedLinks = links.Select(ml =>
            {
                Link? inDb = context.Link.FirstOrDefault(l =>
                    l.LinkProvider == ml.LinkProvider && l.LinkUrl == ml.LinkUrl);
                return inDb ?? ml;
            });
            manga.Links = mergedLinks.ToList();
            IEnumerable<Link> newLinks = manga.Links.Where(ml => !context.Link.Any(l =>
                l.LinkProvider == ml.LinkProvider && l.LinkUrl == ml.LinkUrl));
            context.Link.AddRange(newLinks);
        }

        if (altTitles is not null)
        {
            IEnumerable<MangaAltTitle> mergedAltTitles = altTitles.Select(mat =>
            {
                MangaAltTitle? inDb = context.AltTitles.FirstOrDefault(at =>
                    at.Language == mat.Language && at.Title == mat.Title);
                return inDb ?? mat;
            });
            manga.AltTitles = mergedAltTitles.ToList();
            IEnumerable<MangaAltTitle> newAltTitles = manga.AltTitles.Where(mat =>
                !context.AltTitles.Any(at => at.Language == mat.Language && at.Title == mat.Title));
            context.AltTitles.AddRange(newAltTitles);
        }
        
        existing?.UpdateWithInfo(manga);
        if(existing is not null)
            context.Manga.Update(existing);
        else
            context.Manga.Add(manga);

        context.Jobs.Add(new DownloadMangaCoverJob(manga.MangaId));
        context.Jobs.Add(new RetrieveChaptersJob(0, manga.MangaId));

        context.SaveChanges();
        return existing ?? manga;
    }
}