using API.APIEndpointRecords;
using API.Schema.MangaContext;
using API.Workers;
using Asp.Versioning;
using log4net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{version:apiVersion}/[controller]")]
public class WorkerController(ILog Log) : Controller
{
    /// <summary>
    /// Returns all <see cref="BaseWorker"/>
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<BaseWorker[]>(Status200OK, "application/json")]
    public IActionResult GetAllWorkers()
    {
        return Ok(Tranga.Workers.ToArray());
    }
    
    /// <summary>
    /// Returns <see cref="BaseWorker"/> with requested <paramref name="WorkerIds"/>
    /// </summary>
    /// <param name="WorkerIds">Array of <see cref="BaseWorker"/>.Key</param>
    /// <response code="200"></response>
    [HttpPost("WithIDs")]
    [ProducesResponseType<BaseWorker[]>(Status200OK, "application/json")]
    public IActionResult GetJobs([FromBody]string[] WorkerIds)
    {
        return Ok(Tranga.Workers.Where(worker => WorkerIds.Contains(worker.Key)).ToArray());
    }

    /// <summary>
    /// Get all <see cref="BaseWorker"/> in requested <see cref="WorkerExecutionState"/>
    /// </summary>
    /// <param name="State">Requested <see cref="WorkerExecutionState"/></param>
    /// <response code="200"></response>
    [HttpGet("State/{State}")]
    [ProducesResponseType<BaseWorker[]>(Status200OK, "application/json")]
    public IActionResult GetJobsInState(WorkerExecutionState State)
    {
        return Ok(Tranga.Workers.Where(worker => worker.State == State).ToArray());
    }

    /// <summary>
    /// Return <see cref="BaseWorker"/> with <paramref name="WorkerId"/>
    /// </summary>
    /// <param name="WorkerId"><see cref="BaseWorker"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="BaseWorker"/> with <paramref name="WorkerId"/> could not be found</response>
    [HttpGet("{WorkerId}")]
    [ProducesResponseType<BaseWorker>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetJob(string WorkerId)
    {
        if(Tranga.Workers.FirstOrDefault(w => w.Key == WorkerId) is not { } worker)
            return NotFound(nameof(WorkerId));
        return Ok(worker);
    }

    /// <summary>
    /// Delete <see cref="BaseWorker"/> with <paramref name="WorkerId"/> and all child-<see cref="BaseWorker"/>s
    /// </summary>
    /// <param name="WorkerId"><see cref="BaseWorker"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="BaseWorker"/> with <paramref name="WorkerId"/> could not be found</response>
    [HttpDelete("{WorkerId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult DeleteJob(string WorkerId)
    {
        if(Tranga.Workers.FirstOrDefault(w => w.Key == WorkerId) is not { } worker)
            return NotFound(nameof(WorkerId));
        Tranga.RemoveWorker(worker);
        return Ok();
    }

    /// <summary>
    /// Modify Job with ID
    /// </summary>
    /// <param name="JobId">Job-ID</param>
    /// <param name="modifyJobRecord">Fields to modify, set to null to keep previous value</param>
    /// <response code="202">Job modified</response>
    /// <response code="400">Malformed request</response>
    /// <response code="404">Job with ID not found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPatch("{JobId}")]
    [ProducesResponseType<Job>(Status202Accepted, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult ModifyJob(string JobId, [FromBody]ModifyJobRecord modifyJobRecord)
    {
        try
        {
            Job? ret = context.Jobs.Find(JobId);
            if(ret is null)
                return NotFound();
            
            ret.RecurrenceMs = modifyJobRecord.RecurrenceMs ?? ret.RecurrenceMs;
            ret.Enabled = modifyJobRecord.Enabled ?? ret.Enabled;

            context.SaveChanges();
            return new AcceptedResult(ret.Key, ret);
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Starts the Job with the requested ID
    /// </summary>
    /// <param name="JobId">Job-ID</param>
    /// <param name="startDependencies">Start Jobs necessary for execution</param>
    /// <response code="202">Job started</response>
    /// <response code="404">Job with ID not found</response>
    /// <response code="409">Job was already running</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{JobId}/Start")]
    [ProducesResponseType(Status202Accepted)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status409Conflict)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult StartJob(string JobId, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)]bool startDependencies = false)
    {
        Job? ret = context.Jobs.Find(JobId);
        if (ret is null)
            return NotFound();
        List<Job> dependencies = startDependencies ? ret.GetDependenciesAndSelf() : [ret];
        
        try
        {
            if(dependencies.Any(d => d.state >= JobState.Running && d.state < JobState.Completed))
                return new ConflictResult();
            dependencies.ForEach(d =>
            {
                d.LastExecution = DateTime.UnixEpoch;
                d.state = JobState.CompletedWaiting;
            });
            context.SaveChanges();
            return Accepted();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Stops the Job with the requested ID
    /// </summary>
    /// <param name="JobId">Job-ID</param>
    /// <remarks><h1>NOT IMPLEMENTED</h1></remarks>
    [HttpPost("{JobId}/Stop")]
    [ProducesResponseType(Status501NotImplemented)]
    public IActionResult StopJob(string JobId)
    {
        return StatusCode(Status501NotImplemented);
    }

    /// <summary>
    /// Removes failed and completed Jobs (that are not recurring)
    /// </summary>
    /// <response code="202">Job started</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("Cleanup")]
    public IActionResult CleanupJobs()
    {
        try
        {
            context.Jobs.RemoveRange(context.Jobs.Where(j => j.state == JobState.Failed || j.state == JobState.Completed));
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