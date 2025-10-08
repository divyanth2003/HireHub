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
    public class UserController : ControllerBase
    {
        private readonly UserService _userService;

        public UserController(UserService userService)
        {
            _userService = userService;
        }

     
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] CreateUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _userService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.UserId }, created);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var auth = await _userService.LoginAsync(dto);
            if (auth == null) return Unauthorized(new { message = "Invalid credentials" });

            return Ok(auth);
        }


        public class ForgotPasswordDto
        {
            public string Email { get; set; } = "";
            public string? OriginBaseUrl { get; set; } = null;
        }

        public class ResetPasswordDto
        {
            public string Token { get; set; } = "";
            public string NewPassword { get; set; } = "";
        }

      
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Email))
                return BadRequest(new { message = "Email required" });

            await _userService.RequestPasswordResetAsync(dto.Email, dto.OriginBaseUrl ?? $"{Request.Scheme}://{Request.Host.Value}");
            return Ok(new { message = "If this email is registered, password reset instructions have been sent." });
        }

   
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto?.Token) || string.IsNullOrWhiteSpace(dto?.NewPassword))
                return BadRequest(new { message = "Token and new password required" });

            var ok = await _userService.ResetPasswordWithTokenAsync(dto.Token, dto.NewPassword);
            if (!ok) return BadRequest(new { message = "Invalid or expired token" });
            return Ok(new { message = "Password updated" });
        }

      
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            return Ok(users);
        }

        [Authorize(Roles = "Admin,Employer,JobSeeker")]
        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userService.GetByIdAsync(id);
            return Ok(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("by-role/{role}")]
        public async Task<IActionResult> GetByRole(string role)
        {
            var users = await _userService.GetByRoleAsync(role);
            return Ok(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("search")]
        public async Task<IActionResult> SearchByName([FromQuery] string name)
        {
            var users = await _userService.SearchByNameAsync(name);
            return Ok(users);
        }

        [Authorize(Roles = "Admin,Employer,JobSeeker")]
        [HttpPut("{id:guid}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _userService.UpdateAsync(id, dto);
            return Ok(updated);
        }

        
        [Authorize(Roles = "Admin,Employer,JobSeeker")]
        [HttpDelete("{id:guid}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _userService.DeleteAsync(id);
            return NoContent();
        }
    }
}
