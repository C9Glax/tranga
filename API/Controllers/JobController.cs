using API.Schema;
using API.Schema.Jobs;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{v:apiVersion}/[controller]")]
public class JobController(PgsqlContext context) : Controller
{
    [HttpGet]
    public IActionResult GetJobs([FromBody] string[] ids)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("Types")]
    public IActionResult GetJobTypes()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
    
    [HttpGet("States")]
    public IActionResult GetJobStates()
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }

    [HttpGet("State/{state}")]
    public IActionResult GetJobsInState(string state)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }

    [HttpGet("Type/{type}")]
    public IActionResult GetJobsOfType(string type)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }

    [HttpGet("{id}")]
    public IActionResult GetJob(string id)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }

    [HttpPost("Create")]
    public IActionResult CreateJob([FromBody]Job job)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteJob(string id)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }

    [HttpPost("{id}/Start")]
    public IActionResult StartJob(string id)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }

    [HttpPost("{id}/Stop")]
    public IActionResult StopJob(string id)
    {
        return NotFound(new ProblemResponse("Not implemented"));
    }
}