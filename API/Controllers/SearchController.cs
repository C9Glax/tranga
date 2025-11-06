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
public class SearchController(MangaContext context) : ControllerBase
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
        if (!connector.Enabled)
            return TypedResults.StatusCode(Status412PreconditionFailed);
        
        (Manga manga, Schema.MangaContext.MangaConnectorId<Manga> id)[] mangas = connector.SearchManga(Query);

        IEnumerable<(Manga manga, Schema.MangaContext.MangaConnectorId<Manga> id)> addedManga =
            mangas.Select(kv => context.AddMangaToContext(kv, HttpContext.RequestAborted))
                .Where(t => t.Result is not null)
                .Select(t => t.Result)
                .Cast<(Manga manga, Schema.MangaContext.MangaConnectorId<Manga> id)>();
        IEnumerable<MinimalManga> result = addedManga.Select(manga => manga.manga).Select(m =>
        {
            IEnumerable<DTOs.MangaConnectorId<DTOs.Manga>> ids = m.MangaConnectorIds.Select(id =>
                new DTOs.MangaConnectorId<DTOs.Manga>(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
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
    [HttpGet]
    [ProducesResponseType<MinimalManga>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok<MinimalManga>, NotFound<string>, InternalServerError<string>>> GetMangaFromUrl([FromQuery]string url)
    {
        url = url.Trim('"', '\'', ' '); //Trim extraneous values
        if(Tranga.MangaConnectors.FirstOrDefault(c => c.Name.Equals("Global", StringComparison.InvariantCultureIgnoreCase)) is not { } connector)
            return TypedResults.InternalServerError("Could not find Global Connector.");

        if(connector.GetMangaFromUrl(url) is not ({ } m, not null) manga)
            return TypedResults.NotFound("Could not retrieve Manga");
        
        if(await context.AddMangaToContext(manga, HttpContext.RequestAborted) is not { } added)
            return TypedResults.InternalServerError("Could not add Manga to context");  
        
        IEnumerable<DTOs.MangaConnectorId<DTOs.Manga>> ids = added.manga.MangaConnectorIds.Select(id =>
            new DTOs.MangaConnectorId<DTOs.Manga>(id.Key, id.MangaConnectorName, id.ObjId, id.WebsiteUrl, id.UseForDownload));
        MinimalManga result = new (added.manga.Key, added.manga.Name, added.manga.Description, added.manga.ReleaseStatus, ids);

        return TypedResults.Ok(result);
    }
}