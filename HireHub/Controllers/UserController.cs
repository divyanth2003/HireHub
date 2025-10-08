using System;
using System.Security.Claims;
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
            if (!IsCallerAdmin() && !IsCallerUser(id))
                return Forbid();
            await _userService.DeleteAsync(id);
            return NoContent();
        }

        [Authorize(Roles = "Admin,Employer,JobSeeker")]
        [HttpPost("{id:guid}/schedule-deletion")]
        public async Task<IActionResult> ScheduleDeletion(Guid id, [FromQuery] int days = 30)
        {
            if (days <= 0) days = 30;
            if (!IsCallerAdmin() && !IsCallerUser(id))
                return Forbid();
            var result = await _userService.ScheduleDeletionAsync(id, days);
            return Ok(new { message = result });
        }

        [Authorize(Roles = "Admin,Employer,JobSeeker")]
        [HttpPost("{id:guid}/deactivate")]
        public async Task<IActionResult> Deactivate(Guid id)
        {
            if (!IsCallerAdmin() && !IsCallerUser(id))
                return Forbid();
            var ok = await _userService.DeactivateAccountAsync(id);
            if (!ok) return NotFound(new { message = "User not found or already deactivated" });
            return Ok(new { message = "Account deactivated successfully." });
        }

        [Authorize(Roles = "Admin,Employer,JobSeeker")]
        [HttpPost("{id:guid}/reactivate")]
        public async Task<IActionResult> Reactivate(Guid id)
        {
            if (!IsCallerAdmin() && !IsCallerUser(id))
                return Forbid();
            var ok = await _userService.ReactivateAccountAsync(id);
            if (!ok) return NotFound(new { message = "User not found or already active" });
            return Ok(new { message = "Account reactivated successfully." });
        }

        [Authorize(Roles = "Admin,Employer,JobSeeker")]
        [HttpDelete("{id:guid}/delete-permanently")]
        public async Task<IActionResult> DeletePermanently(Guid id)
        {
            if (!IsCallerAdmin() && !IsCallerUser(id))
                return Forbid();
            var ok = await _userService.DeletePermanentlyAsync(id);
            if (!ok) return NotFound(new { message = "User not found" });
            return Ok(new { message = "Account permanently deleted" });
        }

        private bool IsCallerUser(Guid id)
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(claim)) return false;
            if (!Guid.TryParse(claim, out var callerId)) return false;
            return callerId == id;
        }

        private bool IsCallerAdmin()
        {
            return User.IsInRole("Admin");
        }
    }
}
