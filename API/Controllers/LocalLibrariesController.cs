using API.APIEndpointRecords;
using API.Schema;
using API.Schema.Contexts;
using Asp.Versioning;
using log4net;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class LocalLibrariesController(PgsqlContext context, ILog Log) : Controller
{
    [HttpGet]
    [ProducesResponseType<LocalLibrary[]>(Status200OK, "application/json")]
    public IActionResult GetLocalLibraries()
    {
        return Ok(context.LocalLibraries);
    }

    [HttpGet("{LibraryId}")]
    [ProducesResponseType<LocalLibrary>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetLocalLibrary(string LibraryId)
    {
        LocalLibrary? library = context.LocalLibraries.Find(LibraryId);
        if (library is null)
            return NotFound();
        return Ok(library);
    }

    [HttpPatch("{LibraryId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult UpdateLocalLibrary(string LibraryId, [FromBody]NewLibraryRecord record)
    {
        LocalLibrary? library = context.LocalLibraries.Find(LibraryId);
        if (library is null)
            return NotFound();
        if (record.Validate() == false)
            return BadRequest();
        
        try
        {
            library.LibraryName = record.name;
            library.BasePath = record.path;
            context.SaveChanges();

            return Ok();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }

    [HttpPatch("{LibraryId}/ChangeBasePath")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult ChangeLibraryBasePath(string LibraryId, [FromBody] string newBasePath)
    {
        try
        {
            LocalLibrary? library = context.LocalLibraries.Find(LibraryId);
            if (library is null)
                return NotFound();

            if (false) //TODO implement path check
                return BadRequest();
            
            library.BasePath = newBasePath;
            context.SaveChanges();

            return Ok();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }
    
    [HttpPatch("{LibraryId}/ChangeName")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult ChangeLibraryName(string LibraryId, [FromBody] string newName)
    {
        try
        {
            LocalLibrary? library = context.LocalLibraries.Find(LibraryId);
            if (library is null)
                return NotFound();

            if(newName.Length < 1)
                return BadRequest();
            
            library.LibraryName = newName;
            context.SaveChanges();

            return Ok();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }

    [HttpPut]
    [ProducesResponseType<LocalLibrary>(Status200OK, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateNewLibrary([FromBody]NewLibraryRecord library)
    {
        if (library.Validate() == false)
            return BadRequest();
        try
        {
            LocalLibrary newLibrary = new (library.path, library.name);
            context.LocalLibraries.Add(newLibrary);
            context.SaveChanges();

            return Ok(newLibrary);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }
    
    [HttpDelete("{LibraryId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult DeleteLocalLibrary(string LibraryId)
    {
        
        try
        {
            LocalLibrary? library = context.LocalLibraries.Find(LibraryId);
            if (library is null)
                return NotFound();
            context.Remove(library);
            context.SaveChanges();

            return Ok();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }
}