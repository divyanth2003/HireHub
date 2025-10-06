// Controllers/ResumeController.cs
using System;
using System.IO;
using System.Threading.Tasks;
using HireHub.API.DTOs;
using HireHub.API.Services;
using HireHub.API.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HireHub.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiversion}/[controller]")]
    public class ResumeController : ControllerBase
    {
        private readonly ResumeService _resumeService;
        private readonly ILogger<ResumeController> _logger;

        public ResumeController(ResumeService resumeService, ILogger<ResumeController> logger)
        {
            _resumeService = resumeService;
            _logger = logger;
        }

        // ------------------- GET -------------------
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var resumes = await _resumeService.GetAllAsync();
            return Ok(resumes);
        }

        [Authorize(Roles = "Admin,JobSeeker,Employer")]
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

        // ------------------- CREATE (multipart/form-data) -------------------
        [Authorize(Roles = "JobSeeker")]
        [HttpPost("upload")]
        [RequestSizeLimit(50_000_000)]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateFromForm([FromForm] ResumeUploadFormDto form)
        {
            // Validate required fields
            if (form.JobSeekerId == Guid.Empty) return BadRequest(new { message = "Invalid jobSeekerId" });
            if (string.IsNullOrWhiteSpace(form.ResumeName)) return BadRequest(new { message = "resumeName is required" });

            string filePath = string.Empty;
            string fileType = string.Empty;

            try
            {
                if (form.File != null)
                {
                    if (form.File.Length == 0) return BadRequest(new { message = "Uploaded file is empty" });

                    var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                    var uploadsRoot = Path.Combine(webRoot, "Uploads");
                    if (!Directory.Exists(uploadsRoot)) Directory.CreateDirectory(uploadsRoot);

                    var ext = Path.GetExtension(form.File.FileName);
                    var uniqueName = $"{Guid.NewGuid()}{ext}";
                    var savePath = Path.Combine(uploadsRoot, uniqueName);

                    using (var stream = new FileStream(savePath, FileMode.Create))
                    {
                        await form.File.CopyToAsync(stream);
                    }

                    filePath = Path.Combine("Uploads", uniqueName).Replace('\\', '/');
                    fileType = (ext ?? string.Empty).TrimStart('.');
                }

                var createDto = new CreateResumeDto
                {
                    JobSeekerId = form.JobSeekerId,
                    ResumeName = form.ResumeName,
                    FilePath = filePath,
                    ParsedSkills = form.ParsedSkills,
                    FileType = fileType,
                    IsDefault = form.IsDefault
                };

                var created = await _resumeService.CreateAsync(createDto);
                return CreatedAtAction(nameof(GetById), new { id = created.ResumeId }, created);
            }
            catch (DuplicateEmailException dex)
            {
                _logger.LogWarning(dex, "Duplicate resume name for jobseeker {JobSeekerId}", form.JobSeekerId);
                return Conflict(new { message = dex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating resume for jobseeker {JobSeekerId}", form.JobSeekerId);
                return StatusCode(500, new { message = "Internal server error while uploading resume." });
            }
        }

        // ------------------- CREATE (JSON metadata-only) -------------------
        [Authorize(Roles = "JobSeeker")]
        [HttpPost("metadata")]
        public async Task<IActionResult> CreateMetadata([FromBody] CreateResumeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var created = await _resumeService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetById), new { id = created.ResumeId }, created);
            }
            catch (DuplicateEmailException dex)
            {
                return Conflict(new { message = dex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating resume metadata");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ------------------- UPDATE -------------------
        [Authorize(Roles = "JobSeeker")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateResumeDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            try
            {
                var updated = await _resumeService.UpdateAsync(id, dto);
                return Ok(updated);
            }
            catch (NotFoundException nf)
            {
                return NotFound(new { message = nf.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating resume {ResumeId}", id);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // ------------------- DELETE -------------------
        [Authorize(Roles = "JobSeeker")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                await _resumeService.DeleteAsync(id);
                return NoContent();
            }
            catch (NotFoundException nf)
            {
                return NotFound(new { message = nf.Message });
            }
            catch (ConflictException cf)
            {
                // 409 Conflict if DB prevents delete
                return Conflict(new { message = cf.Message });
            }
            catch (Exception ex)
            {
                // Log and return 500
                _logger.LogError(ex, "Error deleting resume {ResumeId}", id);
                return StatusCode(500, new { message = "Internal server error while deleting resume" });
            }
        }


        // ------------------- UTILITIES -------------------
        [Authorize(Roles = "JobSeeker")]
        [HttpPost("jobseeker/{jobSeekerId:guid}/set-default/{resumeId:int}")]
        public async Task<IActionResult> SetDefault(Guid jobSeekerId, int resumeId)
        {
            try
            {
                await _resumeService.SetDefaultAsync(jobSeekerId, resumeId);
                return Ok(new { message = "Default resume updated successfully." });
            }
            catch (NotFoundException nf)
            {
                return NotFound(new { message = nf.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting default resume {ResumeId} for {JobSeekerId}", resumeId, jobSeekerId);
                return StatusCode(500, new { message = "Failed to set default resume." });
            }
        }
    }
}