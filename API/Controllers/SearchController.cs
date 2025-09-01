using API.MangaConnectors;
using API.Schema.MangaContext;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class SearchController(MangaContext context) : Controller
{
    /// <summary>
    /// Initiate a search for a <see cref="Manga"/> on <see cref="MangaConnector"/> with searchTerm
    /// </summary>
    /// <param name="MangaConnectorName"><see cref="MangaConnector"/>.Name</param>
    /// <param name="Query">searchTerm</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="MangaConnector"/> with Name not found</response>
    /// <response code="412"><see cref="MangaConnector"/> with Name is disabled</response>
    [HttpGet("{MangaConnectorName}/{Query}")]
    [ProducesResponseType<Manga[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status406NotAcceptable)]
    public IActionResult SearchManga (string MangaConnectorName, string Query)
    {
        if(Tranga.MangaConnectors.FirstOrDefault(c => c.Name.Equals(MangaConnectorName, StringComparison.InvariantCultureIgnoreCase)) is not { } connector)
            return NotFound();
        if (connector.Enabled is false)
            return StatusCode(Status412PreconditionFailed);
        
        (Manga, MangaConnectorId<Manga>)[] mangas = connector.SearchManga(Query);
        List<Manga> retMangas = new();
        foreach ((Manga manga, MangaConnectorId<Manga> mcId) manga in mangas)
        {
            if(Tranga.AddMangaToContext(manga, context, out Manga? add, HttpContext.RequestAborted))
                retMangas.Add(add);
        }

        return Ok(retMangas.ToArray());
    }

    /// <summary>
    /// Returns <see cref="Manga"/> from the <see cref="MangaConnector"/> associated with <paramref name="url"/>
    /// </summary>
    /// <param name="url"></param>
    /// <response code="200"></response>
    /// <response code="300">Multiple <see cref="MangaConnector"/> found for URL</response>
    /// <response code="404"><see cref="Manga"/> not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("Url")]
    [ProducesResponseType<Manga>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status500InternalServerError)]
    public IActionResult GetMangaFromUrl ([FromBody]string url)
    {
        if(Tranga.MangaConnectors.FirstOrDefault(c => c.Name.Equals("Global", StringComparison.InvariantCultureIgnoreCase)) is not { } connector)
            return StatusCode(Status500InternalServerError, "Could not find Global Connector.");

        if(connector.GetMangaFromUrl(url) is not { } manga)
            return NotFound();
        
        if(Tranga.AddMangaToContext(manga, context, out Manga? add, HttpContext.RequestAborted) == false)
            return StatusCode(Status500InternalServerError);
        
        return Ok(add);
    }
}