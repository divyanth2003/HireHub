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
    public class EmployerController : ControllerBase
    {
        private readonly EmployerService _employerService;

        public EmployerController(EmployerService employerService)
        {
            _employerService = employerService;
        }


       [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var employers = await _employerService.GetAllAsync();
            return Ok(employers);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var employer = await _employerService.GetByIdAsync(id);
            return Ok(employer);
        }

        [Authorize(Roles = "Admin,Employer")]
        [HttpGet("by-user/{userId:guid}")]
        public async Task<IActionResult> GetByUserId(Guid userId)
        {
            var employer = await _employerService.GetByUserIdAsync(userId);
            return Ok(employer);
        }

        [AllowAnonymous] 
        [HttpGet("search")]
        public async Task<IActionResult> SearchByCompany([FromQuery] string company)
        {
            var employers = await _employerService.SearchByCompanyNameAsync(company);
            return Ok(employers);
        }

        [AllowAnonymous] 
        [HttpGet("by-job/{jobId:int}")]
        public async Task<IActionResult> GetByJobId(int jobId)
        {
            var employer = await _employerService.GetByJobIdAsync(jobId);
            return Ok(employer);
        }

       [Authorize(Roles = "Admin,Employer")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateEmployerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _employerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.EmployerId }, created);
        }

      
      [Authorize(Roles = "Admin,Employer")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateEmployerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _employerService.UpdateAsync(id, dto);
            return Ok(updated);
        }

   
       [Authorize(Roles = "Admin,Employer")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _employerService.DeleteAsync(id);
            return NoContent();
        }
    }
}
