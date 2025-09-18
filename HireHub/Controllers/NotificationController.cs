using System;
using System.Threading.Tasks;
using System.Security.Claims;
using HireHub.API.DTOs;
using HireHub.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace HireHub.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class NotificationController : ControllerBase
    {
        private readonly NotificationService _notificationService;

        public NotificationController(NotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // ------------------- GET -------------------
        [Authorize(Roles = "Employer,JobSeeker,Admin")]
        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> GetByUser(Guid userId)
        {
            var notifs = await _notificationService.GetByUserAsync(userId);
            return Ok(notifs);
        }

        [Authorize(Roles = "Employer,JobSeeker,Admin")]
        [HttpGet("user/{userId:guid}/unread")]
        public async Task<IActionResult> GetUnread(Guid userId)
        {
            var notifs = await _notificationService.GetUnreadByUserAsync(userId);
            return Ok(notifs);
        }

        [Authorize(Roles = "Employer,JobSeeker,Admin")]
        [HttpGet("user/{userId:guid}/recent")]
        public async Task<IActionResult> GetRecent(Guid userId, [FromQuery] int limit = 20)
        {
            var notifs = await _notificationService.GetRecentByUserAsync(userId, limit);
            return Ok(notifs);
        }

        [Authorize(Roles = "Employer,JobSeeker,Admin")]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var notif = await _notificationService.GetByIdAsync(id);
            return Ok(notif);
        }

        [Authorize(Roles = "Employer")]
        [HttpPost("application/message")]
        public async Task<IActionResult> MessageApplicant([FromBody] EmployerNotifyApplicantDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!Guid.TryParse(userIdClaim, out var employerUserId))
                return Forbid();

            var notif = await _notificationService.NotifyApplicantByApplicationAsync(dto, employerUserId);
            return CreatedAtAction(nameof(GetById), new { id = notif.NotificationId }, notif);
        }

        // ------------------- CREATE -------------------
        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateNotificationDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var created = await _notificationService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.NotificationId }, created);
        }

        // ------------------- UPDATE -------------------
        [Authorize(Roles = "Employer,JobSeeker,Admin")]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateNotificationDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var updated = await _notificationService.UpdateAsync(id, dto);
            return Ok(updated);
        }

        [Authorize(Roles = "Employer,JobSeeker,Admin")]
        [HttpPost("{id:int}/mark-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var success = await _notificationService.MarkAsReadAsync(id);
            if (!success) return NotFound();
            return NoContent();
        }

        [Authorize(Roles = "Employer,JobSeeker,Admin")]
        [HttpPost("user/{userId:guid}/mark-all-read")]
        public async Task<IActionResult> MarkAllAsRead(Guid userId)
        {
            var count = await _notificationService.MarkAllAsReadAsync(userId);
            return Ok(new { Updated = count });
        }

        // ------------------- DELETE -------------------
        [Authorize(Roles = "Admin")]
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _notificationService.DeleteAsync(id);
            return NoContent();
        }
    }
}
