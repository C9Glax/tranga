using API.Controllers.Requests;
using API.Schema.MangaContext;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Microsoft.AspNetCore.Http.StatusCodes;
using FileLibrary = API.Controllers.DTOs.FileLibrary;

// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class FileLibraryController(MangaContext context) : ControllerBase
{
    /// <summary>
    /// Returns all <see cref="DTOs.FileLibrary"/>
    /// </summary>
    /// <response code="200"></response>
    /// <response code="500">Error during Database Operation</response>
    [HttpGet]
    [ProducesResponseType<List<FileLibrary>>(Status200OK, "application/json")]
    [ProducesResponseType(Status500InternalServerError)]
    public async Task<Results<Ok<List<FileLibrary>>, InternalServerError>> GetFileLibraries ()
    {
        if (await context.FileLibraries.ToListAsync(HttpContext.RequestAborted) is not { } result)
            return TypedResults.InternalServerError();

        List<FileLibrary> fileLibraries = result.Select(f => new FileLibrary(f.Key, f.BasePath, f.LibraryName)).ToList();

        return TypedResults.Ok(fileLibraries);
    }

    /// <summary>
    /// Returns <see cref="FileLibrary"/> with <paramref name="FileLibraryId"/>
    /// </summary>
    /// <param name="FileLibraryId"><see cref="FileLibrary"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="FileLibrary"/> with <paramref name="FileLibraryId"/> not found.</response>
    [HttpGet("{FileLibraryId}")]
    [ProducesResponseType<FileLibrary>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public async Task<Results<Ok<FileLibrary>, NotFound<string>>> GetFileLibrary (string FileLibraryId)
    {
        if(await context.FileLibraries.FirstOrDefaultAsync(l => l.Key == FileLibraryId, HttpContext.RequestAborted) is not { } library)
            return TypedResults.NotFound(nameof(FileLibraryId));
        
        return TypedResults.Ok(new FileLibrary(library.Key, library.BasePath, library.LibraryName));
    }

    /// <summary>
    /// Changes the <see cref="FileLibraryId"/>.BasePath with <paramref name="FileLibraryId"/>
    /// </summary>
    /// <param name="FileLibraryId"><see cref="FileLibrary"/>.Key</param>
    /// <param name="newBasePath">New <see cref="FileLibraryId"/>.BasePath</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="FileLibrary"/> with <paramref name="FileLibraryId"/> not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("{FileLibraryId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok, NotFound<string>, InternalServerError<string>>> ChangeLibraryBasePath (string FileLibraryId, [FromBody]PatchFileLibraryRecord requestData)
    {
        if(await context.FileLibraries.FirstOrDefaultAsync(l => l.Key == FileLibraryId, HttpContext.RequestAborted) is not { } library)
            return TypedResults.NotFound(nameof(FileLibraryId));

        if (requestData.Path is { } path)
            library.BasePath = path;
        if(requestData.Name is { } name)
            library.LibraryName = name;
        
        if(await context.Sync(HttpContext.RequestAborted, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Ok();
    }

    public record PatchFileLibraryRecord(string? Path, string? Name)
    {
        /// <summary>
        /// Directory Path
        /// </summary>
        public required string? Path { get; init; } = Path;
        /// <summary>
        /// Library Name
        /// </summary>
        public required string? Name { get; init; } = Name;
    }
    
    /// <summary>
    /// Creates new <see cref="FileLibrary"/>
    /// </summary>
    /// <param name="requestData">New <see cref="FileLibrary"/> to add</param>
    /// <response code="200">Key of new Library</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut]
    [ProducesResponseType<string>(Status201Created, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Created<string>, InternalServerError<string>>> CreateNewLibrary ([FromBody]CreateLibraryRecord requestData)
    {
        //TODO Parameter check
        Schema.MangaContext.FileLibrary library = new (requestData.BasePath, requestData.LibraryName);
        context.FileLibraries.Add(library);
        
        if(await context.Sync(HttpContext.RequestAborted, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        
        return TypedResults.Created(string.Empty, library.Key);
    }
    
    /// <summary>
    /// Deletes the <see cref="FileLibraryId"/>.LibraryName with <paramref name="FileLibraryId"/>
    /// </summary>
    /// <param name="FileLibraryId"><see cref="FileLibrary"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="FileLibrary"/> with <paramref name="FileLibraryId"/> not found.</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{FileLibraryId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public async Task<Results<Ok, NotFound<string>, InternalServerError<string>>> DeleteLocalLibrary (string FileLibraryId)
    {
        if(await context.FileLibraries.Where(l => l.Key == FileLibraryId).ExecuteDeleteAsync(HttpContext.RequestAborted) < 1)
            return TypedResults.NotFound(nameof(FileLibraryId));
        
        if(await context.Sync(HttpContext.RequestAborted, GetType(), System.Reflection.MethodBase.GetCurrentMethod()?.Name) is { success: false } result)
            return TypedResults.InternalServerError(result.exceptionMessage);
        return TypedResults.Ok();
    }
}