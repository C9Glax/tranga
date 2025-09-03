using API.Controllers.DTOs;
using API.Schema.MangaContext;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
using Manga = API.Schema.MangaContext.Manga;

// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class SearchController(MangaContext context) : Controller
{
    /// <summary>
    /// Initiate a search for a <see cref="Schema.MangaContext.Manga"/> on <see cref="MangaConnector"/> with searchTerm
    /// </summary>
    /// <param name="MangaConnectorName"><see cref="MangaConnector"/>.Name</param>
    /// <param name="Query">searchTerm</param>
    /// <response code="200"><see cref="MinimalManga"/> exert of <see cref="Schema.MangaContext.Manga"/></response>
    /// <response code="404"><see cref="MangaConnector"/> with Name not found</response>
    /// <response code="412"><see cref="MangaConnector"/> with Name is disabled</response>
    [HttpGet("{MangaConnectorName}/{Query}")]
    [ProducesResponseType<List<MinimalManga>>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType(Status406NotAcceptable)]
    public Results<Ok<List<MinimalManga>>, NotFound<string>, StatusCodeHttpResult> SearchManga (string MangaConnectorName, string Query)
    {
        if(Tranga.MangaConnectors.FirstOrDefault(c => c.Name.Equals(MangaConnectorName, StringComparison.InvariantCultureIgnoreCase)) is not { } connector)
            return TypedResults.NotFound(nameof(MangaConnectorName));
        if (connector.Enabled is false)
            return TypedResults.StatusCode(Status412PreconditionFailed);
        
        (Manga manga, MangaConnectorId<Manga> id)[] mangas = connector.SearchManga(Query);

        IEnumerable<MinimalManga> result = mangas.Select(manga => manga.manga).Select(m =>
        {
            IEnumerable<MangaConnectorId> ids = m.MangaConnectorIds.Select(id =>
                new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
            return new MinimalManga(m.Key, m.Name, m.Description, m.ReleaseStatus, ids);
        });

        return TypedResults.Ok(result.ToList());
    }

    /// <summary>
    /// Returns <see cref="Schema.MangaContext.Manga"/> from the <see cref="MangaConnector"/> associated with <paramref name="url"/>
    /// </summary>
    /// <param name="url"></param>
    /// <response code="200"><see cref="MinimalManga"/> exert of <see cref="Schema.MangaContext.Manga"/>.</response>
    /// <response code="404"><see cref="Manga"/> not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("Url")]
    [ProducesResponseType<MinimalManga>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public Results<Ok<MinimalManga>, NotFound<string>, InternalServerError<string>> GetMangaFromUrl ([FromBody]string url)
    {
        if(Tranga.MangaConnectors.FirstOrDefault(c => c.Name.Equals("Global", StringComparison.InvariantCultureIgnoreCase)) is not { } connector)
            return TypedResults.InternalServerError("Could not find Global Connector.");

        if(connector.GetMangaFromUrl(url) is not { } manga)
            return TypedResults.NotFound("Could not retrieve Manga");
        
        if(Tranga.AddMangaToContext(manga, context, out Manga? m, HttpContext.RequestAborted) == false)
            return TypedResults.InternalServerError("Could not add Manga to context");  
        
        IEnumerable<MangaConnectorId> ids = m.MangaConnectorIds.Select(id =>
            new MangaConnectorId(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
        MinimalManga result = new (m.Key, m.Name, m.Description, m.ReleaseStatus, ids);

        return TypedResults.Ok(result);
    }
}