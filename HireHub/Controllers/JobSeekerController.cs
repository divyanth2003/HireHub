// Controllers/JobSeekerController.cs
using System;
using System.Threading.Tasks;
using HireHub.API.DTOs;
using HireHub.API.Exceptions;
using HireHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HireHub.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiversion}/[controller]")]
    public class JobSeekerController : ControllerBase
    {
        private readonly JobSeekerService _jobSeekerService;
        private readonly ILogger<JobSeekerController> _logger;

        public JobSeekerController(JobSeekerService jobSeekerService, ILogger<JobSeekerController> logger)
        {
            _jobSeekerService = jobSeekerService;
            _logger = logger;
        }

        // ------------------- ADMIN -------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var jobSeekers = await _jobSeekerService.GetAllAsync();
            return Ok(jobSeekers);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var jobSeeker = await _jobSeekerService.GetByIdAsync(id);
            return Ok(jobSeeker);
        }

        // ------------------- BY USER -------------------
        // Allows Admin or the JobSeeker user (or Employer if you permit) to fetch their profile.
        [Authorize(Roles = "Admin,JobSeeker")]
        [HttpGet("by-user/{userId:guid}")]
        public async Task<IActionResult> GetByUserId(Guid userId)
        {
            var jobSeeker = await _jobSeekerService.GetByUserIdAsync(userId);
            return Ok(jobSeeker);
        }

        // ------------------- SEARCH (public / authorized) -------------------
        // Let employers and jobseekers search — make it public if you want anonymous access.
        [AllowAnonymous]
        [HttpGet("search/college")]
        public async Task<IActionResult> SearchByCollege([FromQuery] string name)
        {
            var results = await _jobSeekerService.SearchByCollegeAsync(name);
            return Ok(results);
        }

        [AllowAnonymous]
        [HttpGet("search/skill")]
        public async Task<IActionResult> SearchBySkill([FromQuery] string skill)
        {
            var results = await _jobSeekerService.SearchBySkillAsync(skill);
            return Ok(results);
        }

        // ------------------- CREATE / UPDATE / DELETE -------------------
        // Create a JobSeeker profile (only the JobSeeker user or Admin should create)
        [Authorize(Roles = "Admin,JobSeeker")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateJobSeekerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _jobSeekerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.JobSeekerId }, created);
        }

        // Update profile (Admin or the owning JobSeeker)
        [Authorize(Roles = "Admin,JobSeeker")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobSeekerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _jobSeekerService.UpdateAsync(id, dto);
            return Ok(updated);
        }

        // Delete profile (Admin or owning JobSeeker)
        [Authorize(Roles = "Admin,JobSeeker")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                await _jobSeekerService.DeleteAsync(id);
                return NoContent();
            }
            catch (NotFoundException nf)
            {
                // repository/service told us the resource doesn't exist
                return NotFound(new { message = nf.Message });
            }
            catch (ConflictException cf)
            {
                // delete blocked by dependent data (resumes/applications/etc.)
                return Conflict(new { message = cf.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting jobseeker {JobSeekerId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}