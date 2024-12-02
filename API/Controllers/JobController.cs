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
    /// Returns Jobs with requested Job-IDs
    /// </summary>
    /// <param name="ids">Array of Job-IDs</param>
    /// <returns>Array of Jobs</returns>
    [HttpPost("WithIDs")]
    [ProducesResponseType<Job[]>(Status200OK)]
    public IActionResult GetJobs([FromBody] string[] ids)
    {
        Job[] ret = context.Jobs.Where(job => ids.Contains(job.JobId)).ToArray();
        return Ok(ret);
    }

    /// <summary>
    /// Get all due Jobs (NextExecution > CurrentTime)
    /// </summary>
    /// <returns>Array of Jobs</returns>
    [HttpGet("Due")]
    [ProducesResponseType<Job[]>(Status200OK)]
    public IActionResult GetDueJobs()
    {
        DateTime now = DateTime.Now.ToUniversalTime();
        Job[] dueJobs = context.Jobs.Where(job => job.NextExecution < now && job.state < JobState.Running).ToArray();
        return Ok(dueJobs);
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
    [HttpPost("Type/{type}")]
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
    /// Updates the State of a Job
    /// </summary>
    /// <param name="id">Job-ID</param>
    /// <param name="state">New State</param>
    /// <returns>Nothing</returns>
    [HttpPatch("{id}/Status")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType<string>(Status404NotFound)]
    [ProducesResponseType(Status500InternalServerError)]
    public IActionResult UpdateJobStatus(string id, [FromBody]JobState state)
    {
        try
        {
            Job? ret = context.Jobs.Find(id);
            switch (ret is not null)
            {
                case true:
                    ret.state = state;
                    context.Update(ret);
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
    /// Create a new Job
    /// </summary>
    /// <param name="job">Job</param>
    /// <returns>Nothing</returns>
    [HttpPut]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType<string>(Status500InternalServerError)]
    public IActionResult CreateJob([FromBody]Job job)
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