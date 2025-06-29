using API.Schema;
using API.Schema.Contexts;
using API.Schema.MetadataFetchers;
using Asp.Versioning;
using log4net;
using Microsoft.AspNetCore.Mvc;
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
    /// Tries linking a Manga to a Metadata-Provider-Site
    /// </summary>
    /// <response code="200"></response>
    /// <response code="400">Metadata-fetcher with Name does not exist</response>
    /// <response code="404">Manga with ID not found</response>
    /// <response code="417">Could not find Entry on Metadata-Provider for Manga</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{MetadataFetcherName}/{MangaId}/TryLink")]
    [ProducesResponseType<MetadataEntry>(Status200OK, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status417ExpectationFailed)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult LinkMangaToMetadataFetcher(string MangaId, string MetadataFetcherName)
    {
        if(context.Mangas.Find(MangaId) is not { } manga)
            return NotFound();
        if(Tranga.MetadataFetchers.FirstOrDefault(f => f.MetadataFetcherName == MetadataFetcherName) is not { } fetcher)
            return BadRequest();
        if (!fetcher.TryGetMetadataEntry(manga, out MetadataEntry? entry))
        {
            return StatusCode(Status417ExpectationFailed, "Metadata entry not found");
        }

        try
        {
            //Unlink previous metadata-entries
            IQueryable<MetadataEntry> metadataEntries = context.MetadataEntries.Where(e => e.MangaId == MangaId && e.MetadataFetcherName == MetadataFetcherName);
            context.MetadataEntries.RemoveRange(metadataEntries);
            //Add new metadata-entry
            context.MetadataEntries.Add(entry);
            context.SaveChanges();
            return Ok(entry);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
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