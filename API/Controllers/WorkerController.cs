using API.APIEndpointRecords;
using API.Workers;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{version:apiVersion}/[controller]")]
public class WorkerController() : Controller
{
    /// <summary>
    /// Returns all <see cref="BaseWorker"/>.Keys
    /// </summary>
    /// <response code="200"><see cref="BaseWorker"/> Keys/IDs</response>
    [HttpGet]
    [ProducesResponseType<string[]>(Status200OK, "application/json")]
    public IActionResult GetAllWorkers()
    {
        return Ok(Tranga.AllWorkers.Select(w => w.Key).ToArray());
    }
    
    /// <summary>
    /// Returns <see cref="BaseWorker"/> with requested <paramref name="WorkerIds"/>
    /// </summary>
    /// <param name="WorkerIds">Array of <see cref="BaseWorker"/>.Key</param>
    /// <response code="200"></response>
    [HttpPost("WithIDs")]
    [ProducesResponseType<BaseWorker[]>(Status200OK, "application/json")]
    public IActionResult GetWorkers([FromBody]string[] WorkerIds)
    {
        return Ok(Tranga.AllWorkers.Where(worker => WorkerIds.Contains(worker.Key)).ToArray());
    }

    /// <summary>
    /// Get all <see cref="BaseWorker"/> in requested <see cref="WorkerExecutionState"/>
    /// </summary>
    /// <param name="State">Requested <see cref="WorkerExecutionState"/></param>
    /// <response code="200"></response>
    [HttpGet("State/{State}")]
    [ProducesResponseType<BaseWorker[]>(Status200OK, "application/json")]
    public IActionResult GetWorkersInState(WorkerExecutionState State)
    {
        return Ok(Tranga.AllWorkers.Where(worker => worker.State == State).ToArray());
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
    public IActionResult GetWorker(string WorkerId)
    {
        if(Tranga.AllWorkers.FirstOrDefault(w => w.Key == WorkerId) is not { } worker)
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
    public IActionResult DeleteWorker(string WorkerId)
    {
        if(Tranga.AllWorkers.FirstOrDefault(w => w.Key == WorkerId) is not { } worker)
            return NotFound(nameof(WorkerId));
        Tranga.RemoveWorker(worker);
        return Ok();
    }

    /// <summary>
    /// Modify <see cref="BaseWorker"/> with <paramref name="WorkerId"/>
    /// </summary>
    /// <param name="WorkerId"><see cref="BaseWorker"/>.Key</param>
    /// <param name="modifyWorkerRecord">Fields to modify, set to null to keep previous value</param>
    /// <response code="202"></response>
    /// <response code="400"></response>
    /// <response code="404"><see cref="BaseWorker"/> with <paramref name="WorkerId"/> could not be found</response>
    /// <response code="409"><see cref="BaseWorker"/> is not <see cref="IPeriodic"/>, can not modify <paramref name="modifyWorkerRecord.IntervalMs"/></response>
    [HttpPatch("{WorkerId}")]
    [ProducesResponseType<BaseWorker>(Status202Accepted, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status409Conflict, "text/plain")]
    public IActionResult ModifyWorker(string WorkerId, [FromBody]ModifyWorkerRecord modifyWorkerRecord)
    {
        if(Tranga.AllWorkers.FirstOrDefault(w => w.Key == WorkerId) is not { } worker)
            return NotFound(nameof(WorkerId));
        
        if(modifyWorkerRecord.IntervalMs is not null && worker is not IPeriodic)
            return Conflict("Can not modify Interval of non-Periodic worker");
        else if(modifyWorkerRecord.IntervalMs is not null && worker is IPeriodic periodic)
            periodic.Interval = TimeSpan.FromMilliseconds((long)modifyWorkerRecord.IntervalMs);
        
        return Accepted(worker);
    }

    /// <summary>
    /// Starts <see cref="BaseWorker"/> with <paramref name="WorkerId"/>
    /// </summary>
    /// <param name="WorkerId"><see cref="BaseWorker"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="BaseWorker"/> with <paramref name="WorkerId"/> could not be found</response>
    /// <response code="412"><see cref="BaseWorker"/> was already running</response>
    [HttpPost("{WorkerId}/Start")]
    [ProducesResponseType(Status202Accepted)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status412PreconditionFailed, "text/plain")]
    public IActionResult StartWorker(string WorkerId)
    {
        if(Tranga.AllWorkers.FirstOrDefault(w => w.Key == WorkerId) is not { } worker)
            return NotFound(nameof(WorkerId));
        
        if (worker.State >= WorkerExecutionState.Waiting)
            return StatusCode(Status412PreconditionFailed, "Already running");

        Tranga.MarkWorkerForStart(worker);
        return Ok();
    }

    /// <summary>
    /// Stops <see cref="BaseWorker"/> with <paramref name="WorkerId"/>
    /// </summary>
    /// <param name="WorkerId"><see cref="BaseWorker"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="BaseWorker"/> with <paramref name="WorkerId"/> could not be found</response>
    /// <response code="208"><see cref="BaseWorker"/> was not running</response>
    [HttpPost("{WorkerId}/Stop")]
    [ProducesResponseType(Status501NotImplemented)]
    public IActionResult StopWorker(string WorkerId)
    {
        if(Tranga.AllWorkers.FirstOrDefault(w => w.Key == WorkerId) is not { } worker)
            return NotFound(nameof(WorkerId));
        
        if(worker.State is < WorkerExecutionState.Running or >= WorkerExecutionState.Completed)
            return StatusCode(Status208AlreadyReported, "Not running");
        
        Tranga.StopWorker(worker);
        return Ok();
    }
}