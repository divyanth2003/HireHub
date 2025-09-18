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
    public class ResumeController : ControllerBase
    {
        private readonly ResumeService _resumeService;

        public ResumeController(ResumeService resumeService)
        {
            _resumeService = resumeService;
        }

        // ------------------- GET -------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var resumes = await _resumeService.GetAllAsync();
            return Ok(resumes);
        }

        [Authorize(Roles = "Admin,JobSeeker")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var resume = await _resumeService.GetByIdAsync(id);
            return Ok(resume);
        }

        [Authorize(Roles = "Admin,JobSeeker")]
        [HttpGet("jobseeker/{jobSeekerId:guid}")]
        public async Task<IActionResult> GetByJobSeeker(Guid jobSeekerId)
        {
            var resumes = await _resumeService.GetByJobSeekerAsync(jobSeekerId);
            return Ok(resumes);
        }

        [Authorize(Roles = "Admin,JobSeeker")]
        [HttpGet("jobseeker/{jobSeekerId:guid}/default")]
        public async Task<IActionResult> GetDefault(Guid jobSeekerId)
        {
            var resume = await _resumeService.GetDefaultByJobSeekerAsync(jobSeekerId);
            if (resume == null) return NotFound(new { message = "No default resume set." });
            return Ok(resume);
        }

        // ------------------- CREATE -------------------
        [Authorize(Roles = "JobSeeker")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateResumeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _resumeService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.ResumeId }, created);
        }

        // ------------------- UPDATE -------------------
        [Authorize(Roles = "JobSeeker")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateResumeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _resumeService.UpdateAsync(id, dto);
            return Ok(updated);
        }

        // ------------------- DELETE -------------------
        [Authorize(Roles = "JobSeeker")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _resumeService.DeleteAsync(id);
            return NoContent();
        }

        // ------------------- UTILITIES -------------------
        [Authorize(Roles = "JobSeeker")]
        [HttpPost("jobseeker/{jobSeekerId:guid}/set-default/{resumeId:int}")]
        public async Task<IActionResult> SetDefault(Guid jobSeekerId, int resumeId)
        {
            await _resumeService.SetDefaultAsync(jobSeekerId, resumeId);
            return Ok(new { message = "Default resume updated successfully." });
        }
    }
}
