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
    [HttpGet]
    [ProducesResponseType<Job[]>(Status200OK)]
    public IActionResult GetJobs([FromBody] string[] ids)
    {
        Job[] ret = context.Jobs.Where(job => ids.Contains(job.JobId)).ToArray();
        return Ok(ret);
    }
    
    [HttpGet("Types")]
    [ProducesResponseType<string[]>(Status200OK)]
    public IActionResult GetJobTypes()
    {
        return Ok(Enum.GetNames<JobType>());
    }

    [HttpGet("Due")]
    [ProducesResponseType<Job[]>(Status200OK)]
    public IActionResult GetDueJobs()
    {
        DateTime now = DateTime.Now.ToUniversalTime();
        Job[] dueJobs = context.Jobs.Where(job => now < job.NextExecution).ToArray();
        return Ok(dueJobs);
    }
    
    [HttpGet("States")]
    [ProducesResponseType<string[]>(Status200OK)]
    public IActionResult GetJobStates()
    {
        return Ok(Enum.GetNames<JobState>());
    }

    [HttpGet("State/{stateStr}")]
    [ProducesResponseType<Job[]>(Status200OK)]
    [ProducesResponseType<Exception>(Status400BadRequest)]
    public IActionResult GetJobsInState(string stateStr)
    {
        try
        {
            JobState state = Enum.Parse<JobState>(stateStr);
            Job[] jobsInState = context.Jobs.Where(job => job.state == state).ToArray();
            return Ok(jobsInState);
        }
        catch (Exception e)
        {
            return BadRequest(e);
        }
    }

    [HttpGet("Type/{type}")]
    [ProducesResponseType<Job[]>(Status200OK)]
    [ProducesResponseType<Exception>(Status400BadRequest)]
    public IActionResult GetJobsOfType(string typeStr)
    {
        try
        {
            JobType type = Enum.Parse<JobType>(typeStr);
            Job[] jobsOfType = context.Jobs.Where(job => job.JobType == type).ToArray();
            return Ok(jobsOfType);
        }
        catch (Exception e)
        {
            return BadRequest(e);
        }
    }

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
        return NotFound(new ProblemResponse("Not implemented"));
    }
}