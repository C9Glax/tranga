using API.Schema.MangaContext;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class FileLibraryController(MangaContext context) : Controller
{
    /// <summary>
    /// Returns all <see cref="FileLibrary"/>
    /// </summary>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet]
    [ProducesResponseType<FileLibrary[]>(Status200OK, "application/json")]
    public async Task<IActionResult> GetFileLibraries ()
    {
        if(await context.FileLibraries.ToArrayAsync(HttpContext.RequestAborted) is not { } result)
            return StatusCode(Status500InternalServerError);
        return Ok(result);
    }

    /// <summary>
    /// Returns <see cref="FileLibrary"/> with <paramref name="FileLibraryId"/>
    /// </summary>
    /// <param name="FileLibraryId"><see cref="FileLibrary"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="FileLibrary"/> with <paramref name="FileLibraryId"/> not found.</response>
    [HttpGet("{FileLibraryId}")]
    [ProducesResponseType<FileLibrary>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public async Task<IActionResult> GetFileLibrary (string FileLibraryId)
    {
        if(await context.FileLibraries.FirstOrDefaultAsync(l => l.Key == FileLibraryId, HttpContext.RequestAborted) is not { } library)
            return NotFound();
        
        return Ok(library);
    }

    /// <summary>
    /// Changes the <see cref="FileLibraryId"/>.BasePath with <paramref name="FileLibraryId"/>
    /// </summary>
    /// <param name="FileLibraryId"><see cref="FileLibrary"/>.Key</param>
    /// <param name="newBasePath">New <see cref="FileLibraryId"/>.BasePath</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="FileLibrary"/> with <paramref name="FileLibraryId"/> not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("{FileLibraryId}/ChangeBasePath")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<IActionResult> ChangeLibraryBasePath (string FileLibraryId, [FromBody]string newBasePath)
    {
        if(await context.FileLibraries.FirstOrDefaultAsync(l => l.Key == FileLibraryId, HttpContext.RequestAborted) is not { } library)
            return NotFound();
        
        //TODO Path check
        library.BasePath = newBasePath;
        
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return StatusCode(Status500InternalServerError, result.exceptionMessage);
        return Ok();
    }
    
    /// <summary>
    /// Changes the <see cref="FileLibraryId"/>.LibraryName with <paramref name="FileLibraryId"/>
    /// </summary>
    /// <param name="FileLibraryId"><see cref="FileLibrary"/>.Key</param>
    /// <param name="newName">New <see cref="FileLibraryId"/>.LibraryName</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="FileLibrary"/> with <paramref name="FileLibraryId"/> not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("{FileLibraryId}/ChangeName")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<IActionResult> ChangeLibraryName (string FileLibraryId, [FromBody] string newName)
    {
        if(await context.FileLibraries.FirstOrDefaultAsync(l => l.Key == FileLibraryId, HttpContext.RequestAborted) is not { } library)
            return NotFound();
        
        //TODO Name check
        library.LibraryName = newName;
        
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return StatusCode(Status500InternalServerError, result.exceptionMessage);
        return Ok();
    }
    
    /// <summary>
    /// Creates new <see cref="FileLibrary"/>
    /// </summary>
    /// <param name="library">New <see cref="FileLibrary"/> to add</param>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<IActionResult> CreateNewLibrary ([FromBody]FileLibrary library)
    {
        //TODO Parameter check
        context.FileLibraries.Add(library);
        
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return StatusCode(Status500InternalServerError, result.exceptionMessage);
        return Created();
    }
    
    /// <summary>
    /// Deletes the <see cref="FileLibraryId"/>.LibraryName with <paramref name="FileLibraryId"/>
    /// </summary>
    /// <param name="FileLibraryId"><see cref="FileLibrary"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{FileLibraryId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<IActionResult> DeleteLocalLibrary (string FileLibraryId)
    {
        if(await context.FileLibraries.FirstOrDefaultAsync(l => l.Key == FileLibraryId, HttpContext.RequestAborted) is not { } library)
            return NotFound();
        
        context.FileLibraries.Remove(library);
        
        if(await context.Sync(HttpContext.RequestAborted) is { success: false } result)
            return StatusCode(Status500InternalServerError, result.exceptionMessage);
        return Ok();
    }
}