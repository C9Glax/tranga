using API.Controllers.DTOs;
using API.Workers;
using Asp.Versioning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;
// ReSharper disable InconsistentNaming

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{version:apiVersion}/[controller]")]
public class WorkerController : ControllerBase
{
    /// <summary>
    /// Returns all <see cref="BaseWorker"/>
    /// </summary>
    /// <response code="200"><see cref="Worker"/></response>
    [HttpGet]
    [ProducesResponseType<List<Worker>>(Status200OK, "application/json")]
    public Ok<List<Worker>> GetWorkers()
    {
        IEnumerable<Worker> result = Tranga.GetKnownWorkers().Select(w =>
            new Worker(w.Key, w.AllDependencies.Select(d => d.Key), w.MissingDependencies.Select(d => d.Key), w.AllDependenciesFulfilled, w.State));
        return TypedResults.Ok(result.ToList());
    }

    /// <summary>
    /// Get all <see cref="BaseWorker"/> in requested <see cref="WorkerExecutionState"/>
    /// </summary>
    /// <param name="State">Requested <see cref="WorkerExecutionState"/></param>
    /// <response code="200"></response>
    [HttpGet("State/{State}")]
    [ProducesResponseType<List<Worker>>(Status200OK, "application/json")]
    public Ok<List<Worker>> GetWorkersInState(WorkerExecutionState State)
    {
        IEnumerable<Worker> result = Tranga.GetKnownWorkers().Where(worker => worker.State == State).Select(w =>
            new Worker(w.Key, w.AllDependencies.Select(d => d.Key), w.MissingDependencies.Select(d => d.Key), w.AllDependenciesFulfilled, w.State));
        return TypedResults.Ok(result.ToList());
    }

    /// <summary>
    /// Return <see cref="BaseWorker"/> with <paramref name="WorkerId"/>
    /// </summary>
    /// <param name="WorkerId"><see cref="BaseWorker"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="BaseWorker"/> with <paramref name="WorkerId"/> could not be found</response>
    [HttpGet("{WorkerId}")]
    [ProducesResponseType<Worker>(Status200OK, "application/json")]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    public Results<Ok<Worker>, NotFound<string>> GetWorker(string WorkerId)
    {
        if(Tranga.GetKnownWorkers().FirstOrDefault(w => w.Key == WorkerId) is not { } w)
            return TypedResults.NotFound(nameof(WorkerId));
        
        Worker result = new (w.Key, w.AllDependencies.Select(d => d.Key), w.MissingDependencies.Select(d => d.Key), w.AllDependenciesFulfilled, w.State);
        
        return TypedResults.Ok(result);
    }

    /// <summary>
    /// Stops <see cref="BaseWorker"/> with <paramref name="WorkerId"/>
    /// </summary>
    /// <param name="WorkerId"><see cref="BaseWorker"/>.Key</param>
    /// <response code="200"></response>
    /// <response code="404"><see cref="BaseWorker"/> with <paramref name="WorkerId"/> could not be found</response>
    /// <response code="412"><see cref="BaseWorker"/> was already not running</response>
    [HttpPost("{WorkerId}/Stop")]
    [ProducesResponseType(Status202Accepted)]
    [ProducesResponseType<string>(Status404NotFound, "text/plain")]
    [ProducesResponseType(Status412PreconditionFailed)]
    public Results<Ok, NotFound<string>, StatusCodeHttpResult> StopWorker(string WorkerId)
    {
        if(Tranga.GetRunningWorkers().FirstOrDefault(w => w.Key == WorkerId) is not { } worker)
            return TypedResults.NotFound(nameof(WorkerId));
        
        if(worker.State is < WorkerExecutionState.Running or >= WorkerExecutionState.Completed)
            return TypedResults.StatusCode(Status412PreconditionFailed);
        
        Tranga.StopWorker(worker);
        return TypedResults.Ok();
    }
}