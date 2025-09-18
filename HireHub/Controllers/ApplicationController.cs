using System;
using System.Threading.Tasks;
using HireHub.API.DTOs;
using HireHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ApplicationController : ControllerBase
    {
        private readonly ApplicationService _applicationService;

        public ApplicationController(ApplicationService applicationService)
        {
            _applicationService = applicationService;
        }

        // ------------------- GET -------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var apps = await _applicationService.GetAllAsync();
            return Ok(apps);
        }

        [Authorize(Roles = "Admin,Employer,JobSeeker")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var app = await _applicationService.GetByIdAsync(id);
            return Ok(app);
        }

        [Authorize(Roles = "Employer,Admin")]
        [HttpGet("job/{jobId:int}")]
        public async Task<IActionResult> GetByJob(int jobId)
        {
            var apps = await _applicationService.GetByJobAsync(jobId);
            return Ok(apps);
        }

        [Authorize(Roles = "JobSeeker,Admin")]
        [HttpGet("jobseeker/{jobSeekerId:guid}")]
        public async Task<IActionResult> GetByJobSeeker(Guid jobSeekerId)
        {
            var apps = await _applicationService.GetByJobSeekerAsync(jobSeekerId);
            return Ok(apps);
        }

        [Authorize(Roles = "Employer,Admin")]
        [HttpGet("job/{jobId:int}/shortlisted")]
        public async Task<IActionResult> GetShortlisted(int jobId)
        {
            var apps = await _applicationService.GetShortlistedByJobAsync(jobId);
            return Ok(apps);
        }

        [Authorize(Roles = "Employer,Admin")]
        [HttpGet("job/{jobId:int}/interviews")]
        public async Task<IActionResult> GetWithInterview(int jobId)
        {
            var apps = await _applicationService.GetWithInterviewAsync(jobId);
            return Ok(apps);
        }

        // ------------------- CREATE -------------------
        [Authorize(Roles = "JobSeeker")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateApplicationDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _applicationService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.ApplicationId }, created);
        }

        // ------------------- UPDATE -------------------
        [Authorize(Roles = "Employer,Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateApplicationDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _applicationService.UpdateAsync(id, dto);
            return Ok(updated);
        }

        // ------------------- DELETE -------------------
        [Authorize(Roles = "JobSeeker,Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _applicationService.DeleteAsync(id);
            return NoContent();
        }

        // ------------------- UTILITIES -------------------
        [Authorize(Roles = "Employer,Admin")]
        [HttpPost("{appId:int}/review")]
        public async Task<IActionResult> MarkReviewed(int appId, [FromBody] string? notes)
        {
            var reviewed = await _applicationService.MarkReviewedAsync(appId, notes);
            return Ok(reviewed);
        }
    }
}
