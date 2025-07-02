using API.Schema.MangaContext;
using API.Schema.MangaContext.MangaConnectors;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class SearchController(IServiceScope scope) : Controller
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
    public IActionResult SearchManga(string MangaConnectorName, string Query)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if(context.MangaConnectors.Find(MangaConnectorName) is not { } connector)
            return NotFound();
        if (connector.Enabled is false)
            return StatusCode(Status412PreconditionFailed);
        
        (Manga, MangaConnectorId<Manga>)[] mangas = connector.SearchManga(Query);
        List<Manga> retMangas = new();
        foreach ((Manga manga, MangaConnectorId<Manga> mcId) manga in mangas)
        {
            if(AddMangaToContext(manga, context) is { } add)
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
    public IActionResult GetMangaFromUrl([FromBody]string url)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if (context.MangaConnectors.Find("Global") is not { } connector)
            return StatusCode(Status500InternalServerError, "Could not find Global Connector.");

        if(connector.GetMangaFromUrl(url) is not { } manga)
            return NotFound();
        
        if(AddMangaToContext(manga, context) is not { } add)
            return StatusCode(Status500InternalServerError);
        
        return Ok(add);
    }
    
    private Manga? AddMangaToContext((Manga, MangaConnectorId<Manga>) manga, MangaContext context) => AddMangaToContext(manga.Item1, manga.Item2, context);

    private static Manga? AddMangaToContext(Manga addManga, MangaConnectorId<Manga> addMcId, MangaContext context)
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
        
        
        if(context.MangaConnectorToManga.Find(addMcId.Key) is null)
            context.MangaConnectorToManga.Add(mcId);

        if (context.Sync().Result is { success: false } )
            return null;
        return manga;
    }
}