using API.Schema;
using API.Schema.Contexts;
using API.Schema.MetadataFetchers;
using Asp.Versioning;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class MetadataFetcherController(PgsqlContext context, ILog Log) : Controller
{
    /// <summary>
    /// Get all available Connectors (Metadata-Sites)
    /// </summary>
    /// <response code="200">Names of Metadata-Fetchers</response>
    [HttpGet]
    [ProducesResponseType<string[]>(Status200OK, "application/json")]
    public IActionResult GetConnectors()
    {
        string[] connectors = Tranga.MetadataFetchers.Select(f => f.MetadataFetcherName).ToArray();
        return Ok(connectors);
    }

    /// <summary>
    /// Returns all Mangas which have a linked Metadata-Provider
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("Links")]
    [ProducesResponseType<MetadataEntry>(Status200OK, "application/json")]
    public IActionResult GetLinkedEntries()
    {
        return Ok(context.MetadataEntries.ToArray());
    }

    /// <summary>
    /// Searches Metadata-Provider for Manga-Metadata
    /// </summary>
    /// <param name="searchTerm">Instead of using the Manga for search, use a specific term</param>
    /// <response code="200"></response>
    /// <response code="400">Metadata-fetcher with Name does not exist</response>
    /// <response code="404">Manga with ID not found</response>
    [HttpPost("{MetadataFetcherName}/SearchManga/{MangaId}")]
    [ProducesResponseType<MetadataSearchResult[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult SearchMangaMetadata(string MangaId, string MetadataFetcherName, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]string? searchTerm = null)
    {
        if(context.Mangas.Find(MangaId) is not { } manga)
            return NotFound();
        if(Tranga.MetadataFetchers.FirstOrDefault(f => f.MetadataFetcherName == MetadataFetcherName) is not { } fetcher)
            return BadRequest();

        MetadataSearchResult[] searchResults = searchTerm is null ? fetcher.SearchMetadataEntry(manga) : fetcher.SearchMetadataEntry(searchTerm);
        return Ok(searchResults);
    }

    /// <summary>
    /// Links Metadata-Provider using Provider-Specific Identifier to Manga
    /// </summary>
    /// <response code="200"></response>
    /// <response code="400">Metadata-fetcher with Name does not exist</response>
    /// <response code="404">Manga with ID not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{MetadataFetcherName}/Link/{MangaId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult LinkMangaMetadata(string MangaId, string MetadataFetcherName, [FromBody]string Identifier)
    {
        if(context.Mangas.Find(MangaId) is not { } manga)
            return NotFound();
        if(Tranga.MetadataFetchers.FirstOrDefault(f => f.MetadataFetcherName == MetadataFetcherName) is not { } fetcher)
            return BadRequest();
        MetadataEntry entry = fetcher.CreateMetadataEntry(manga, Identifier);
        
        try
        {
            context.MetadataEntries.Add(entry);
            context.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
        return Ok();
    }

    /// <summary>
    /// Un-Links Metadata-Provider using Provider-Specific Identifier to Manga
    /// </summary>
    /// <response code="200"></response>
    /// <response code="400">Metadata-fetcher with Name does not exist</response>
    /// <response code="404">Manga with ID not found</response>
    /// <response code="412">No Entry linking Manga and Metadata-Provider found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{MetadataFetcherName}/Unlink/{MangaId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status412PreconditionFailed, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult UnlinkMangaMetadata(string MangaId, string MetadataFetcherName)
    {
        if(context.Mangas.Find(MangaId) is not { } manga)
            return NotFound();
        if(Tranga.MetadataFetchers.FirstOrDefault(f => f.MetadataFetcherName == MetadataFetcherName) is not { } fetcher)
            return BadRequest();
        MetadataEntry? entry = context.MetadataEntries.FirstOrDefault(e => e.MangaId == MangaId && e.MetadataFetcherName == MetadataFetcherName);
        if (entry is null)
            return StatusCode(Status412PreconditionFailed, "No entry found");
        try
        {
            context.MetadataEntries.Remove(entry);
            context.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
        return Ok();
    }

    /// <summary>
    /// Tries linking a Manga to a Metadata-Provider-Site
    /// </summary>
    /// <response code="200"></response>
    /// <response code="400">MetadataFetcher Name is invalid</response>
    /// <response code="404">Manga has no linked entry with MetadataFetcher</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{MetadataFetcherName}/{MangaId}/UpdateMetadata")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult UpdateMetadata(string MangaId, string MetadataFetcherName)
    {
        if(Tranga.MetadataFetchers
               .FirstOrDefault(f =>
                   f.MetadataFetcherName.Equals(MetadataFetcherName, StringComparison.InvariantCultureIgnoreCase)) is not { } fetcher)
            return BadRequest();
        MetadataEntry? entry = context.MetadataEntries
            .FirstOrDefault(e =>
                e.MangaId == MangaId && e.MetadataFetcherName.Equals(MetadataFetcherName, StringComparison.InvariantCultureIgnoreCase));
        if (entry is null)
            return NotFound();
        try
        {
            fetcher.UpdateMetadata(entry, context);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
        return Ok();
    }
}