using API.Schema;
using API.Schema.Jobs;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Produces("application/json")]
[Route("v{version:apiVersion}/[controller]")]
public class JobController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Returns all Jobs
    /// </summary>
    /// <returns>Array of Jobs</returns>
    [HttpGet]
    [ProducesResponseType<Job[]>(Status200OK)]
    public IActionResult GetAllJobs()
    {
        Job[] ret = context.Jobs.ToArray();
        return Ok(ret);
    }
    
    /// <summary>
    /// Returns Jobs with requested Job-IDs
    /// </summary>
    /// <param name="ids">Array of Job-IDs</param>
    /// <returns>Array of Jobs</returns>
    [HttpPost("WithIDs")]
    [ProducesResponseType<Job[]>(Status200OK)]
    public IActionResult GetJobs([FromBody]string[] ids)
    {
        Job[] ret = context.Jobs.Where(job => ids.Contains(job.JobId)).ToArray();
        return Ok(ret);
    }

    /// <summary>
    /// Get all Jobs in requested State
    /// </summary>
    /// <param name="state">Requested Job-State</param>
    /// <returns>Array of Jobs</returns>
    [HttpGet("State/{state}")]
    [ProducesResponseType<Job[]>(Status200OK)]
    public IActionResult GetJobsInState(JobState state)
    {
        Job[] jobsInState = context.Jobs.Where(job => job.state == state).ToArray();
        return Ok(jobsInState);
    }

    /// <summary>
    /// Returns all Jobs of requested Type
    /// </summary>
    /// <param name="type">Requested Job-Type</param>
    /// <returns>Array of Jobs</returns>
    [HttpGet("Type/{type}")]
    [ProducesResponseType<Job[]>(Status200OK)]
    public IActionResult GetJobsOfType(JobType type)
    {
        Job[] jobsOfType = context.Jobs.Where(job => job.JobType == type).ToArray();
        return Ok(jobsOfType);
    }

    /// <summary>
    /// Return Job with ID
    /// </summary>
    /// <param name="id">Job-ID</param>
    /// <returns>Job</returns>
    [HttpGet("{id}")]
    [ProducesResponseType<Job>(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetJob(string id)
    {
        Job? ret = context.Jobs.Find(id);
        return (ret is not null) switch
        {
            true => Ok(ret),
            false => NotFound()
        };
    }

    /// <summary>
    /// Create a new CreateNewDownloadChapterJob
    /// </summary>
    /// <param name="request">ID of the Manga, and how often we check again</param>
    /// <returns>Nothing</returns>
    [HttpPut("NewDownloadChapterJob/{mangaId}")]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult CreateNewDownloadChapterJob(string mangaId, [FromBody]ulong recurrenceTime)
    {
        Job job = new DownloadNewChaptersJob(recurrenceTime, mangaId);
        return AddJob(job);
    }

    /// <summary>
    /// Create a new DownloadSingleChapterJob
    /// </summary>
    /// <param name="chapterId">ID of the Chapter</param>
    /// <returns>Nothing</returns>
    [HttpPut("DownloadSingleChapterJob/{chapterId}")]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult CreateNewDownloadChapterJob(string chapterId)
    {
        Job job = new DownloadSingleChapterJob(chapterId);
        return AddJob(job);
    }

    /// <summary>
    /// Create a new UpdateMetadataJob
    /// </summary>
    /// <param name="mangaId">ID of the Manga</param>
    /// <returns>Nothing</returns>
    [HttpPut("UpdateMetadataJob/{mangaId}")]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult CreateUpdateMetadataJob(string mangaId)
    {
        Job job = new UpdateMetadataJob(0, mangaId);
        return AddJob(job);
    }

    /// <summary>
    /// Create a new UpdateMetadataJob for all Manga
    /// </summary>
    /// <returns>Nothing</returns>
    [HttpPut("UpdateMetadataJob")]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult CreateUpdateAllMetadataJob()
    {
        List<string> ids = context.Manga.Select(m => m.MangaId).ToList();
        List<UpdateMetadataJob> jobs =  ids.Select(id => new UpdateMetadataJob(0, id)).ToList();
        try
        {
            context.Jobs.AddRange(jobs);
            context.SaveChanges();
            return Created();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }
    
    private IActionResult AddJob(Job job)
    {
        try
        {
            context.Jobs.Add(job);
            context.SaveChanges();
            return Created();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Delete Job with ID
    /// </summary>
    /// <param name="id">Job-ID</param>
    /// <returns>Nothing</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status500InternalServerError)]
    public IActionResult DeleteJob(string id)
    {
        try
        {
            Job? ret = context.Jobs.Find(id);
            switch (ret is not null)
            {
                case true:
                    context.Remove(ret);
                    context.SaveChanges();
                    return Ok();
                case false: return NotFound();
            }
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Starts the Job with the requested ID
    /// </summary>
    /// <param name="id">Job-ID</param>
    /// <returns>Nothing</returns>
    [HttpPost("{id}/Start")]
    [ProducesResponseType(Status202Accepted)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status500InternalServerError)]
    public IActionResult StartJob(string id)
    {
        Job? ret = context.Jobs.Find(id);
        if (ret is null)
            return NotFound();
        ret.NextExecution = DateTime.UnixEpoch;
        try
        {
            context.Update(ret);
            context.SaveChanges();
            return Accepted();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    [HttpPost("{id}/Stop")]
    public IActionResult StopJob(string id)
    {
        return NotFound(new ProblemResponse("Not implemented")); //TODO
    }
}