using System;
using System.Threading.Tasks;
using HireHub.API.DTOs;
using HireHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HireHub.API.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiversion}/[controller]")]
    public class JobController : ControllerBase
    {
        private readonly JobService _jobService;

        public JobController(JobService jobService)
        {
            _jobService = jobService;
        }

       
        [AllowAnonymous] 
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var jobs = await _jobService.GetAllAsync();
            return Ok(jobs);
        }

        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var job = await _jobService.GetByIdAsync(id);
            return Ok(job);
        }

        [Authorize(Roles = "Employer,Admin")]
        [HttpGet("employer/{employerId:guid}")]
        public async Task<IActionResult> GetByEmployer(Guid employerId)
        {
            var jobs = await _jobService.GetByEmployerAsync(employerId);
            return Ok(jobs);
        }

      
        [AllowAnonymous]
        [HttpGet("search/title")]
        public async Task<IActionResult> SearchByTitle([FromQuery] string query)
        {
            var jobs = await _jobService.SearchByTitleAsync(query);
            return Ok(jobs);
        }

        [AllowAnonymous]
        [HttpGet("search/location")]
        public async Task<IActionResult> SearchByLocation([FromQuery] string location)
        {
            var jobs = await _jobService.SearchByLocationAsync(location);
            return Ok(jobs);
        }

        [AllowAnonymous]
        [HttpGet("search/skill")]
        public async Task<IActionResult> SearchBySkill([FromQuery] string skill)
        {
            var jobs = await _jobService.SearchBySkillAsync(skill);
            return Ok(jobs);
        }
        [AllowAnonymous]
        [HttpGet("search/company")]
        public async Task<IActionResult> SearchByCompany([FromQuery] string company)
        {
            var jobs = await _jobService.SearchByCompanyAsync(company);
            return Ok(jobs);
        }


        [Authorize(Roles = "Employer")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateJobDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _jobService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.JobId }, created);
        }

        [Authorize(Roles = "Employer")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateJobDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _jobService.UpdateAsync(id, dto);
            return Ok(updated);
        }

        [Authorize(Roles = "Employer,Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _jobService.DeleteAsync(id);
            return NoContent();
        }
    }
}
