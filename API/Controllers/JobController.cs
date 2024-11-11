using API.Schema;
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

    }
    
    [HttpGet("Types")]
    public IActionResult GetJobTypes()
    {

    }
    
    [HttpGet("States")]
    public IActionResult GetJobStates()
    {

    }

    [HttpGet("State/{state}")]
    public IActionResult GetJobsInState(string state)
    {

    }

    [HttpGet("Type/{type}")]
    public IActionResult GetJobsOfType(string type)
    {

    }

    [HttpGet("{id}")]
    public IActionResult GetJob(string id)
    {

    }

    [HttpPost("Create")]
    public IActionResult CreateJob([FromBody] jobdetails)
    {
        
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteJob(string id)
    {
        
    }

    [HttpPost("{id}/Start")]
    public IActionResult StartJob(string id)
    {
        
    }

    [HttpPost("{id}/Stop")]
    public IActionResult StopJob(string id)
    {
        
    }
}