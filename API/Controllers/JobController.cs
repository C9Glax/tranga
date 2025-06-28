using API.APIEndpointRecords;
using API.Schema;
using API.Schema.Contexts;
using API.Schema.Jobs;
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
public class JobController(PgsqlContext context, ILog Log) : Controller
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
    /// Returns all Jobs of requested Type and State
    /// </summary>
    /// <param name="JobType">Requested Job-Type</param>
    /// <param name="JobState">Requested Job-State</param>
    /// <response code="200"></response>
    [HttpGet("TypeAndState/{JobType}/{JobState}")]
    [ProducesResponseType<Job[]>(Status200OK, "application/json")]
    public IActionResult GetJobsOfType(JobType JobType, JobState JobState)
    {
        Job[] jobsOfType = context.Jobs.Where(job => job.JobType == JobType && job.state == JobState).ToArray();
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
    /// Create a new DownloadAvailableChaptersJob
    /// </summary>
    /// <param name="MangaId">ID of Manga</param>
    /// <param name="record">Job-Configuration</param>
    /// <response code="201">Job-IDs</response>
    /// <response code="400">Could not find ToLibrary with ID</response>
    /// <response code="404">Could not find Manga with ID</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("DownloadAvailableChaptersJob/{MangaId}")]
    [ProducesResponseType<string[]>(Status201Created, "application/json")]
    [ProducesResponseType(Status400BadRequest)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateDownloadAvailableChaptersJob(string MangaId, [FromBody]DownloadAvailableChaptersJobRecord record)
    {
        if (context.Mangas.Find(MangaId) is not { } m)
            return NotFound();
        else
        {
            try
            {
                LocalLibrary? l = context.LocalLibraries.Find(record.localLibraryId);
                if (l is null)
                    return BadRequest();
                m.Library = l;
                context.SaveChanges();
            }
            catch (Exception e)
            {
                Log.Error(e);
                return StatusCode(500, e.Message);
            }
        }
        Job retrieveChapters = new RetrieveChaptersJob(m, record.language, record.recurrenceTimeMs);
        Job updateFilesDownloaded =
            new UpdateChaptersDownloadedJob(m, record.recurrenceTimeMs, dependsOnJobs: [retrieveChapters]);
        Job downloadChapters = new DownloadAvailableChaptersJob(m, record.recurrenceTimeMs, dependsOnJobs: [retrieveChapters, updateFilesDownloaded]);
        Job UpdateCover = new UpdateCoverJob(m, record.recurrenceTimeMs, downloadChapters);
        retrieveChapters.ParentJob = downloadChapters;
        updateFilesDownloaded.ParentJob = retrieveChapters;
        return AddJobs([retrieveChapters, downloadChapters, updateFilesDownloaded, UpdateCover]);
    }

    /// <summary>
    /// Create a new DownloadSingleChapterJob
    /// </summary>
    /// <param name="ChapterId">ID of the Chapter</param>
    /// <response code="201">Job-IDs</response>
    /// <response code="404">Could not find Chapter with ID</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("DownloadSingleChapterJob/{ChapterId}")]
    [ProducesResponseType<string[]>(Status201Created, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateNewDownloadChapterJob(string ChapterId)
    {
        if(context.Chapters.Find(ChapterId) is not { } c)
            return NotFound();
        Job job = new DownloadSingleChapterJob(c);
        return AddJobs([job]);
    }

    /// <summary>
    /// Create a new UpdateChaptersDownloadedJob
    /// </summary>
    /// <param name="MangaId">ID of the Manga</param>
    /// <response code="201">Job-IDs</response>
    /// <response code="201">Could not find Manga with ID</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("UpdateFilesJob/{MangaId}")]
    [ProducesResponseType<string[]>(Status201Created, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateUpdateFilesDownloadedJob(string MangaId)
    {
        if(context.Mangas.Find(MangaId) is not { } m)
            return NotFound();
        Job job = new UpdateChaptersDownloadedJob(m, 0);
        return AddJobs([job]);
    }

    /// <summary>
    /// Create a new UpdateMetadataJob for all Manga
    /// </summary>
    /// <response code="201">Job-IDs</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("UpdateAllFilesJob")]
    [ProducesResponseType<string[]>(Status201Created, "application/json")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateUpdateAllFilesDownloadedJob()
    {
        List<UpdateChaptersDownloadedJob> jobs = context.Mangas.Select(m => new UpdateChaptersDownloadedJob(m, 0, null, null)).ToList();
        try
        {
            context.Jobs.AddRange(jobs);
            context.SaveChanges();
            return Created();
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Not Implemented: Create a new UpdateMetadataJob
    /// </summary>
    /// <param name="MangaId">ID of the Manga</param>
    /// <response code="201">Job-IDs</response>
    /// <response code="404">Could not find Manga with ID</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("UpdateMetadataJob/{MangaId}")]
    [ProducesResponseType<string[]>(Status201Created, "application/json")]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateUpdateMetadataJob(string MangaId)
    {
        return StatusCode(Status501NotImplemented);
    }

    /// <summary>
    /// Not Implemented: Create a new UpdateMetadataJob for all Manga
    /// </summary>
    /// <response code="201">Job-IDs</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpPut("UpdateAllMetadataJob")]
    [ProducesResponseType<string[]>(Status201Created, "application/json")]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult CreateUpdateAllMetadataJob()
    {
        return StatusCode(Status501NotImplemented);
    }
    
    private IActionResult AddJobs(Job[] jobs)
    {
        try
        {
            context.Jobs.AddRange(jobs);
            context.SaveChanges();
            return new CreatedResult((string?)null, jobs.Select(j => j.JobId).ToArray());
        }
        catch (Exception e)
        {
            Log.Error(e);
            return StatusCode(500, e.Message);
        }
    }

    /// <summary>
    /// Delete Job with ID and all children
    /// </summary>
    /// <param name="JobId">Job-ID</param>
    /// <response code="200"></response>
    /// <response code="404">Job could not be found</response>
    /// <response code="500">Error during Database Operation</response>
    [HttpDelete("{JobId}")]
    [ProducesResponseType(Status200OK)]
    [ProducesResponseType(Status404NotFound)]
    [ProducesResponseType<string>(Status500InternalServerError, "text/plain")]
    public IActionResult DeleteJob(string JobId)
    {
        try
        {
            if(context.Jobs.Find(JobId) is not { } ret)
                return NotFound();
            
            context.Remove(ret);
            context.SaveChanges();
            return Ok();
        }
        catch (Exception e)
        {
            Log.Error(e);
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