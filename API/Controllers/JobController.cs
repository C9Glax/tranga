﻿using API.APIEndpointRecords;
using API.Schema;
using API.Schema.Jobs;
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using static Microsoft.AspNetCore.Http.StatusCodes;

namespace API.Controllers;

[ApiVersion(2)]
[ApiController]
[Route("v{version:apiVersion}/[controller]")]
public class JobController(PgsqlContext context) : Controller
{
    /// <summary>
    /// Returns all Jobs
    /// </summary>
    /// <response code="200"></response>
    [HttpGet]
    [ProducesResponseType<Job[]>(Status200OK, "application/json")]
    public IActionResult GetAllJobs()
    {
        Job[] ret = context.Jobs.ToArray();
        return Ok(ret);
    }
    
    /// <summary>
    /// Returns Jobs with requested Job-IDs
    /// </summary>
    /// <param name="ids">Array of Job-IDs</param>
    /// <response code="200"></response>
    [HttpPost("WithIDs")]
    [ProducesResponseType<Job[]>(Status200OK, "application/json")]
    public IActionResult GetJobs([FromBody]string[] ids)
    {
        Job[] ret = context.Jobs.Where(job => ids.Contains(job.JobId)).ToArray();
        return Ok(ret);
    }

    /// <summary>
    /// Get all Jobs in requested State
    /// </summary>
    /// <param name="JobState">Requested Job-State</param>
    /// <response code="200"></response>
    [HttpGet("State/{JobState}")]
    [ProducesResponseType<Job[]>(Status200OK, "application/json")]
    public IActionResult GetJobsInState(JobState JobState)
    {
        Job[] jobsInState = context.Jobs.Where(job => job.state == JobState).ToArray();
        return Ok(jobsInState);
    }

    /// <summary>
    /// Returns all Jobs of requested Type
    /// </summary>
    /// <param name="JobType">Requested Job-Type</param>
    /// <response code="200"></response>
    [HttpGet("Type/{JobType}")]
    [ProducesResponseType<Job[]>(Status200OK, "application/json")]
    public IActionResult GetJobsOfType(JobType JobType)
    {
        Job[] jobsOfType = context.Jobs.Where(job => job.JobType == JobType).ToArray();
        return Ok(jobsOfType);
    }

    /// <summary>
    /// Return Job with ID
    /// </summary>
    /// <param name="JobId">Job-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Job with ID could not be found</response>
    [HttpGet("{JobId}")]
    [ProducesResponseType<Job>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    public IActionResult GetJob(string JobId)
    {
        Job? ret = context.Jobs.Find(JobId);
        return (ret is not null) switch
        {
            true => Ok(ret),
            false => NotFound()
        };
    }

    /// <summary>
    /// Create a new CreateNewDownloadChapterJob
    /// </summary>
    /// <param name="MangaId">ID of Manga</param>
    /// <param name="recurrenceTime">How often should we check for new chapters</param>
    /// <response code="201">Created new Job</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("NewDownloadChapterJob/{MangaId}")]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateNewDownloadChapterJob(string MangaId, [FromBody]ulong recurrenceTime)
    {
        Job job = new DownloadNewChaptersJob(recurrenceTime, MangaId);
        return AddJob(job);
    }

    /// <summary>
    /// Create a new DownloadSingleChapterJob
    /// </summary>
    /// <param name="ChapterId">ID of the Chapter</param>
    /// <response code="201">Created new Job</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("DownloadSingleChapterJob/{ChapterId}")]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateNewDownloadChapterJob(string ChapterId)
    {
        Job job = new DownloadSingleChapterJob(ChapterId);
        return AddJob(job);
    }

    /// <summary>
    /// Create a new UpdateMetadataJob
    /// </summary>
    /// <param name="MangaId">ID of the Manga</param>
    /// <response code="201">Created new Job</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("UpdateMetadataJob/{MangaId}")]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateUpdateMetadataJob(string MangaId)
    {
        Job job = new UpdateMetadataJob(0, MangaId);
        return AddJob(job);
    }

    /// <summary>
    /// Create a new UpdateMetadataJob for all Manga
    /// </summary>
    /// <response code="201">Created new Job</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("UpdateMetadataJob")]
    [ProducesResponseType(Status201Created)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
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
            return new CreatedResult(job.JobId, job);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Delete Job with ID and all children
    /// </summary>
    /// <param name="JobId">Job-ID</param>
    /// <response code="200">Job(s) deleted</response>
    /// <response code="404">Job could not be found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{JobId}")]
    [ProducesResponseType<string[]>(Status200OK, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult DeleteJob(string JobId)
    {
        try
        {
            Job? ret = context.Jobs.Find(JobId);
            if(ret is null)
                return NotFound();
            IQueryable<Job> children = GetChildJobs(JobId);
            
            context.RemoveRange(children);
            context.Remove(ret);
            context.SaveChanges();
            return new OkObjectResult(children.Select(x => x.JobId).Append(ret.JobId).ToArray());
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    private IQueryable<Job> GetChildJobs(string parentJobId)
    {
        IQueryable<Job> children = context.Jobs.Where(j => j.ParentJobId == parentJobId);
        foreach (Job child in children)
            foreach (Job grandChild in GetChildJobs(child.JobId))
                children.Append(grandChild);
        return children;
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
            return new AcceptedResult(ret.JobId, ret);
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Starts the Job with the requested ID
    /// </summary>
    /// <param name="JobId">Job-ID</param>
    /// <response code="202">Job started</response>
    /// <response code="404">Job with ID not found</response>
    /// <response code="409">Job was already running</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPost("{JobId}/Start")]
    [ProducesResponseType(Status202Accepted)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType(Status409Conflict)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult StartJob(string JobId)
    {
        Job? ret = context.Jobs.Find(JobId);
        if (ret is null)
            return NotFound();
        try
        {
            if (ret.state >= JobState.Running && ret.state < JobState.Completed)
                return new ConflictResult();
            ret.LastExecution = DateTime.UnixEpoch;
            context.SaveChanges();
            return Accepted();
        }
        catch (Exception e)
        {
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Stops the Job with the requested ID
    /// </summary>
    /// <param name="JobId">Job-ID</param>
    /// <remarks>NOT IMPLEMENTED</remarks>
    [HttpPost("{JobId}/Stop")]
    public IActionResult StopJob(string JobId)
    {
        throw new NotImplementedException();
    }
}