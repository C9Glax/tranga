using API.Schema.MangaContext;
using API.Schema.MangaContext.MetadataFetchers;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class MetadataFetcherController(IServiceScope scope) : Controller
{
    /// <summary>
    /// Get all <see cref="MetadataFetcher"/> (Metadata-Sites)
    /// </summary>
    /// <response code="200">Names of <see cref="MetadataFetcher"/> (Metadata-Sites)</response>
    [HttpGet]
    [ProducesResponseType<string[]>(Status200OK, "application/json")]
    public IActionResult GetConnectors()
    {
        return Ok(Tranga.MetadataFetchers.Select(m => m.Name).ToArray());
    }

    /// <summary>
    /// Returns all <see cref="MetadataEntry"/>
    /// </summary>
    /// <response code="200"></response>
    [HttpGet("Links")]
    [ProducesResponseType<MetadataEntry[]>(Status200OK, "application/json")]
    public IActionResult GetLinkedEntries()
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        
        return Ok(context.MetadataEntries.ToArray());
    }

    /// <summary>
    /// Searches <see cref="MetadataFetcher"/> (Metadata-Sites) for Manga-Metadata
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <param name="MetadataFetcherName"><see cref="MetadataFetcher"/>.Name</param>
    /// <param name="searchTerm">Instead of using the <paramref name="MangaId"/> for search on Website, use a specific term</param>
    /// <response code="200"></response>
    /// <response code="400"><see cref="MetadataFetcher"/> (Metadata-Sites) with <paramref name="MetadataFetcherName"/> does not exist</response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    [HttpPost("{MetadataFetcherName}/SearchManga/{MangaId}")]
    [ProducesResponseType<MetadataSearchResult[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult SearchMangaMetadata(string MangaId, string MetadataFetcherName, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]string? searchTerm = null)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if(context.Mangas.Find(MangaId) is not { } manga)
            return NotFound();
        if(Tranga.MetadataFetchers.FirstOrDefault(f => f.Name == MetadataFetcherName) is not { } fetcher)
            return BadRequest();

        MetadataSearchResult[] searchResults = searchTerm is null ? fetcher.SearchMetadataEntry(manga) : fetcher.SearchMetadataEntry(searchTerm);
        return Ok(searchResults);
    }

    /// <summary>
    /// Links <see cref="MetadataFetcher"/> (Metadata-Sites) using Provider-Specific Identifier to <see cref="Manga"/>
    /// </summary>
    /// <param name="MangaId"><see cref="Manga"/>.Key</param>
    /// <param name="MetadataFetcherName"><see cref="MetadataFetcher"/>.Name</param>
    /// <param name="Identifier"><see cref="MetadataFetcherName"/>-Specific ID</param>
    /// <response code="200"></response>
    /// <response code="400"><see cref="MetadataFetcher"/> (Metadata-Sites) with <paramref name="MetadataFetcherName"/> does not exist</response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{MetadataFetcherName}/Link/{MangaId}")]
    [ProducesResponseType<MetadataEntry>(Status200OK, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult LinkMangaMetadata(string MangaId, string MetadataFetcherName, [FromBody]string Identifier)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if(context.Mangas.Find(MangaId) is not { } manga)
            return NotFound();
        if(Tranga.MetadataFetchers.FirstOrDefault(f => f.Name == MetadataFetcherName) is not { } fetcher)
            return BadRequest();
        
        MetadataEntry entry = fetcher.CreateMetadataEntry(manga, Identifier);
        context.MetadataEntries.Add(entry);
        
        if(context.Sync().Result is { } errorMessage)
            return StatusCode(Status500InternalServerError, errorMessage);
        return Ok(entry);
    }

    /// <summary>
    /// Un-Links <see cref="MetadataFetcher"/> (Metadata-Sites) from <see cref="Manga"/>
    /// </summary>
    /// <response code="200"></response>
    /// <response code="400"><see cref="MetadataFetcher"/> (Metadata-Sites) with <paramref name="MetadataFetcherName"/> does not exist</response>
    /// <response code="404"><see cref="Manga"/> with <paramref name="MangaId"/> not found</response>
    /// <response code="412">No <see cref="MetadataEntry"/> linking <see cref="Manga"/> and <see cref="MetadataFetcher"/> found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{MetadataFetcherName}/Unlink/{MangaId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status412PreconditionFailed, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult UnlinkMangaMetadata(string MangaId, string MetadataFetcherName)
    {
        MangaContext context = scope.ServiceProvider.GetRequiredService<MangaContext>();
        if(context.Mangas.Find(MangaId) is null)
            return NotFound();
        if(Tranga.MetadataFetchers.FirstOrDefault(f => f.Name == MetadataFetcherName) is null)
            return BadRequest();
        if(context.MetadataEntries.FirstOrDefault(e => e.MangaId == MangaId && e.MetadataFetcherName == MetadataFetcherName) is not { } entry)
            return StatusCode(Status412PreconditionFailed, "No entry found");

        context.Remove(entry);
        
        if(context.Sync().Result is { success: false } result)
            return StatusCode(Status500InternalServerError, result.exceptionMessage);
        return Ok();
    }
}